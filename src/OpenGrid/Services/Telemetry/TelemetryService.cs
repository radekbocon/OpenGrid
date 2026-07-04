using Serilog;
using OpenGrid.Models;

namespace OpenGrid.Services.Telemetry;

/// <summary>
/// Service for reading telemetry data from shared files written by the bridge
/// Polls the shared memory files continuously for new data
/// </summary>
public class TelemetryService : ITelemetryService
{
    private const int PollIntervalMs = 1000 / 120;

    private readonly List<ITelemetryClient> _telemetryClients;
    private readonly SharedMemoryBridgeLauncher _sharedMemoryBridgeLauncher;
    private readonly SteamWatcher _steamWatcher;

    private ITelemetryClient? _telemetryClient;

    private CancellationTokenSource? _cts;
    private bool _disposed;

    public event EventHandler<TelemetryEventArgs>? TelemetryReceived;
    public event EventHandler<TelemetryConnectionStatus>? TelemetryStatusChanged;

    public TelemetryConnectionStatus ConnectionStatus
    {
        get;
        private set
        {
            field = value;
            TelemetryStatusChanged?.Invoke(this, value);
        }
    }

    public SteamGame? CurrentGame { get; private set; }

    public TelemetryService(IEnumerable<ITelemetryClient> telemetryClients,
        SharedMemoryBridgeLauncher sharedMemoryBridgeLauncher, SteamWatcher steamWatcher)
    {
        _telemetryClients = telemetryClients.ToList();
        _sharedMemoryBridgeLauncher = sharedMemoryBridgeLauncher;
        _steamWatcher = steamWatcher;

        _steamWatcher.GameStopped += SteamWatcherOnGameStopped;
    }

    public async Task<bool> ConnectAsync(SteamGame game)
    {
        try
        {
            _disposed = false;
            _cts = new CancellationTokenSource();
            CurrentGame = game;
            ConnectionStatus = TelemetryConnectionStatus.Connecting;

            _telemetryClient = _telemetryClients.First(x => x.GetType() == game.TelemetryClientType);

            if (game.RequiresSharedMemoryBridge)
            {
                await _sharedMemoryBridgeLauncher.LaunchBridgeAsync(game, _cts.Token);
            }

            var result = await _telemetryClient.ConnectAsync(_cts.Token);
            ConnectionStatus = result ? TelemetryConnectionStatus.Connected : TelemetryConnectionStatus.Disconnected;
            
            StartReading();

            return ConnectionStatus == TelemetryConnectionStatus.Connected;
        }
        catch (Exception e)
        {
            ConnectionStatus = TelemetryConnectionStatus.Error;
            Log.Error(e, "Error connecting to telemetry: {0}", e.Message);
            return false;
        }
    }
    
    public void Disconnect()
    {
        try
        {
            ConnectionStatus = TelemetryConnectionStatus.Disconnected;
            _sharedMemoryBridgeLauncher.StopBridge();
            _telemetryClient?.Stop();
            _telemetryClient = null;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
        catch (ObjectDisposedException)
        {
            // ignore
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Disconnect();
        _disposed = true;
    }

    private void StartReading()
    {
        if (ConnectionStatus != TelemetryConnectionStatus.Connected)
        {
            Log.Warning("Cannot start reading telemetry: not connected");
            return;
        }

        try
        {
            _ = ReadPollingLoopAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error starting reading telemetry: {0}", ex.Message);
            ConnectionStatus = TelemetryConnectionStatus.Disconnected;
        }
    }

    private void SteamWatcherOnGameStopped(SteamGameProcess gameProcess)
    {
        if (CurrentGame?.AppId == gameProcess.SteamGame.AppId)
        {
            Dispose();
        }
    }

    private async Task ReadPollingLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var snapshot = _telemetryClient?.ReadTelemetry();
                if (snapshot != null && CurrentGame != null)
                {
                    TelemetryReceived?.Invoke(this, new TelemetryEventArgs(CurrentGame, snapshot));
                }

                await Task.Delay(PollIntervalMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                ConnectionStatus = TelemetryConnectionStatus.Disconnected;
                break;
            }
            catch (Exception)
            {
                ConnectionStatus = TelemetryConnectionStatus.Disconnected;
                await Task.Delay(1000, cancellationToken); // Wait longer on error
            }
        }
    }
}

public enum TelemetryConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    Error
}