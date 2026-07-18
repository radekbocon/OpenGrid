using System.Net.WebSockets;
using System.Text;
using OpenGrid.Models.Telemetry;

namespace OpenGrid.Services.Dashboard;

public sealed class TelemetryWebSocketBroadcaster : IDisposable
{
    private const int BroadcastIntervalMs = 33;

    private readonly Lock _lock = new();
    private readonly List<WebSocket> _connections = [];
    private readonly ICarConfigService _carConfigService;

    private TelemetryRecord? _latestSnapshot;
    private CancellationTokenSource? _cts;
    private int? _cachedRedlineRpm;
    private float? _cachedBrakeBiasOffset;
    private string? _lastCarKey;

    public TelemetryWebSocketBroadcaster(ICarConfigService carConfigService)
    {
        _carConfigService = carConfigService;
        _carConfigService.CarUpdated += (_, car) =>
        {
            _cachedRedlineRpm = car.RedlineRpm;
            _cachedBrakeBiasOffset = car.BrakeBiasOffset;
        };
    }

    public void Start()
    {
        if (_cts is not null)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _ = BroadcastLoopAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        lock (_lock)
        {
            foreach (var ws in _connections)
            {
                try
                {
                    ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", CancellationToken.None);
                }
                catch
                {
                    // ignore
                }
            }
            _connections.Clear();
        }
    }

    public void Dispose()
    {
        Stop();
    }

    public void OnTelemetryReceived(TelemetryRecord record)
    {
        _latestSnapshot = record;

        var carKey = record.Car.Key;
        if (carKey != _lastCarKey)
        {
            _lastCarKey = carKey;
            var profile = _carConfigService.GetByCarKey(carKey);
            _cachedRedlineRpm = profile?.RedlineRpm;
            _cachedBrakeBiasOffset = profile?.BrakeBiasOffset;
        }
    }

    public async Task HandleConnectionAsync(WebSocket webSocket)
    {
        lock (_lock)
        {
            _connections.Add(webSocket);
        }

        try
        {
            var buffer = new byte[1024];
            while (webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                catch (WebSocketException)
                {
                    break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    try
                    {
                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    }
                    catch
                    {
                        // ignore
                    }
                    break;
                }
            }
        }
        finally
        {
            lock (_lock)
            {
                _connections.Remove(webSocket);
            }

            try
            {
                webSocket.Dispose();
            }
            catch
            {
                // ignore
            }
        }
    }

    private async Task BroadcastLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var snapshot = Interlocked.Exchange(ref _latestSnapshot, null);
            if (snapshot is not null)
            {
                var adjustedSnapshot = _cachedBrakeBiasOffset is { } offset
                    ? snapshot with { BrakeBias = snapshot.BrakeBias + offset }
                    : snapshot;

                var json = TelemetryJsonSerializer.Serialize(adjustedSnapshot, _cachedRedlineRpm);
                var bytes = Encoding.UTF8.GetBytes(json);

                WebSocket[] connections;
                lock (_lock)
                {
                    connections = _connections.ToArray();
                }

                foreach (var ws in connections)
                {
                    if (ws.State != WebSocketState.Open)
                        continue;

                    try
                    {
                        await ws.SendAsync(
                            new ArraySegment<byte>(bytes),
                            WebSocketMessageType.Text,
                            endOfMessage: true,
                            ct);
                    }
                    catch
                    {
                        // Will be removed when the receive loop detects the disconnect
                    }
                }
            }

            try
            {
                await Task.Delay(BroadcastIntervalMs, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
