using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using SimLab.Models;

namespace SimLab.Services;

public class DashboardService : IDashboardService
{
    private readonly ITelemetryService _telemetryService;
    private readonly ISettingsService _settingsService;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private DashboardInfo? _activeDashboard;
    private readonly ConcurrentDictionary<WebSocket, byte> _connectedSockets = [];
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<WebSocket, byte>> _deviceSockets = new(StringComparer.OrdinalIgnoreCase);

    public bool IsRunning => _listener is not null;
    public int Port { get; private set; }
    public DashboardInfo? ActiveDashboard => _activeDashboard;

    private string LocalUrl => $"http://127.0.0.1:{Port}";
    private string NetworkUrl => $"http://{GetLocalIpAddress()}:{Port}";

    public DashboardService(IDashboardRepository repository, ITelemetryService telemetryService, ISettingsService settingsService)
    {
        _telemetryService = telemetryService;
        _settingsService = settingsService;
    }

    public void Start(DashboardInfo dashboard)
    {
        _activeDashboard = dashboard;
        EnsureRunning();
    }

    public string GetUrl(bool useNetwork)
    {
        return useNetwork ? NetworkUrl : LocalUrl;
    }

    public void OpenInBrowser()
    {
        if (_activeDashboard is null) return;
        var url = GetUrl(useNetwork: false);
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to open dashboard in browser");
        }
    }

    public void OpenInWebView()
    {
        OpenInBrowser();
    }

    public void Stop()
    {
        if (!IsRunning) return;

        _telemetryService.TelemetryReceived -= OnTelemetryReceived;
        _cts?.Cancel();
        _listener?.Stop();
        _listener = null;

        foreach (var kvp in _connectedSockets)
        {
            try
            {
                kvp.Key.Dispose();
            }
            catch
            {
                // ignored
            }
        }
        _connectedSockets.Clear();
        _deviceSockets.Clear();

        Log.Information("Dashboard HTTP server stopped");
    }

    private void EnsureRunning()
    {
        if (IsRunning) return;

        var savedPort = _settingsService.DashboardPort;
        if (savedPort.HasValue && IsPortAvailable(savedPort.Value))
        {
            Port = savedPort.Value;
        }
        else
        {
            Port = FindFreePort();
        }

        _listener = new TcpListener(IPAddress.Any, Port);
        _listener.Start();
        _settingsService.DashboardPort = Port;
        _cts = new CancellationTokenSource();
        _ = AcceptConnectionsAsync(_cts.Token);
        _telemetryService.TelemetryReceived += OnTelemetryReceived;
        Log.Information("Dashboard HTTP server started on port {Port}", Port);
    }

    private static bool IsPortAvailable(int port)
    {
        try
        {
            using var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void OnTelemetryReceived(object? sender, TelemetryEventArgs e)
    {
        if (_connectedSockets.IsEmpty) return;

        var payload = JsonSerializer.Serialize(new
        {
            type = "telemetry",
            data = new
            {
                speed = e.Telemetry.SpeedKmh,
                rpm = e.Telemetry.EngineRpm,
                maxRpm = e.Telemetry.MaxRpm,
                gear = e.Telemetry.CurrentGear,
                gas = e.Telemetry.Gas,
                brake = e.Telemetry.Brake,
                clutch = e.Telemetry.Clutch,
                steerAngle = e.Telemetry.SteerAngle,
                fuel = e.Telemetry.Fuel,
                car = e.Telemetry.Car,
                track = e.Telemetry.Track,
                sessionType = e.Telemetry.SessionType.ToString(),
                currentLap = e.Telemetry.CurrentLap,
                lapTime = e.Telemetry.LapTime > TimeSpan.Zero ? e.Telemetry.LapTime.ToString(@"mm\:ss\.fff") : "--:--",
                lastLapTime = e.Telemetry.LapTime > TimeSpan.Zero ? e.Telemetry.LastLapTime.ToString(@"mm\:ss\.fff") : "--:--",
                bestLapTime = e.Telemetry.LapTime > TimeSpan.Zero ? e.Telemetry.BestLapTime.ToString(@"mm\:ss\.fff") : "--:--",
                tireTemps = new
                {
                    fl = e.Telemetry.TireTemperatures.FrontLeft,
                    fr = e.Telemetry.TireTemperatures.FrontRight,
                    rl = e.Telemetry.TireTemperatures.RearLeft,
                    rr = e.Telemetry.TireTemperatures.RearRight
                },
                tirePressures = new
                {
                    fl = e.Telemetry.TirePressures.FrontLeft,
                    fr = e.Telemetry.TirePressures.FrontRight,
                    rl = e.Telemetry.TirePressures.RearLeft,
                    rr = e.Telemetry.TirePressures.RearRight
                },
                abs = e.Telemetry.Abs,
                tc1 = e.Telemetry.Tc1
            }
        });

        var bytes = Encoding.UTF8.GetBytes(payload);
        var segment = new ArraySegment<byte>(bytes);

        var deadSockets = new List<WebSocket>();
        foreach (var kvp in _connectedSockets)
        {
            var ws = kvp.Key;
            try
            {
                if (ws.State == WebSocketState.Open)
                {
                    ws.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None).Wait(100);
                }
                else
                {
                    deadSockets.Add(ws);
                }
            }
            catch
            {
                deadSockets.Add(ws);
            }
        }

        foreach (var dead in deadSockets)
        {
            _connectedSockets.TryRemove(dead, out _);
            RemoveDeviceSocket(dead);
            try
            {
                dead.Dispose();
            }
            catch
            {
                // ignored
            }
        }
    }

    private async Task AcceptConnectionsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var client = await _listener!.AcceptTcpClientAsync(ct);
                _ = HandleConnectionAsync(client, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error accepting dashboard connection");
            }
        }
    }

    private async Task HandleConnectionAsync(TcpClient client, CancellationToken ct)
    {
        var remoteIp = ((IPEndPoint)client.Client.RemoteEndPoint!).Address.ToString();
        try
        {
            using var stream = client.GetStream();
            var buffer = new byte[8192];
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct);
            if (bytesRead == 0) return;

            var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            var lines = request.Split("\r\n");
            if (lines.Length == 0) return;

            var requestLine = lines[0];
            var parts = requestLine.Split(' ');
            if (parts.Length < 2) return;

            var method = parts[0];
            var path = parts[1];

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrEmpty(line)) break;
                var colonIdx = line.IndexOf(':');
                if (colonIdx > 0)
                {
                    var key = line[..colonIdx].Trim();
                    var value = line[(colonIdx + 1)..].Trim();
                    headers[key] = value;
                }
            }

            if (method == "GET" && path == "/ws" &&
                headers.TryGetValue("Upgrade", out var upgrade) &&
                upgrade.Equals("websocket", StringComparison.OrdinalIgnoreCase))
            {
                await HandleWebSocketUpgrade(stream, headers, ct, remoteIp);
                return;
            }

            if (method == "GET")
            {
                await ServeFile(stream, path, ct);
            }
            else
            {
                await WriteResponse(stream, 405, "Method Not Allowed", "text/plain", "Method Not Allowed", ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error handling dashboard connection from {Ip}", remoteIp);
        }
        finally
        {
            client.Close();
        }
    }

    private void RemoveDeviceSocket(WebSocket ws)
    {
        foreach (var kvp in _deviceSockets)
        {
            if (kvp.Value.TryRemove(ws, out _) && kvp.Value.IsEmpty)
                _deviceSockets.TryRemove(kvp.Key, out _);
        }
    }

    private async Task HandleWebSocketUpgrade(NetworkStream stream, Dictionary<string, string> headers, CancellationToken ct, string remoteIp)
    {
        if (!headers.TryGetValue("Sec-WebSocket-Key", out var key))
        {
            await WriteResponse(stream, 400, "Bad Request", "text/plain", "Missing Sec-WebSocket-Key", ct);
            return;
        }

        var acceptKey = ComputeWebSocketAcceptKey(key);
        var response = $"HTTP/1.1 101 Switching Protocols\r\n" +
                       $"Upgrade: websocket\r\n" +
                       $"Connection: Upgrade\r\n" +
                       $"Sec-WebSocket-Accept: {acceptKey}\r\n" +
                       $"Access-Control-Allow-Origin: *\r\n\r\n";

        var responseBytes = Encoding.UTF8.GetBytes(response);
        await stream.WriteAsync(responseBytes, 0, responseBytes.Length, ct);
        await stream.FlushAsync(ct);

        var ws = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions
        {
            IsServer = true,
            KeepAliveInterval = TimeSpan.FromSeconds(30)
        });

        _connectedSockets.TryAdd(ws, 0);

        var deviceSockets = _deviceSockets.GetOrAdd(remoteIp, _ => new ConcurrentDictionary<WebSocket, byte>());
        deviceSockets.TryAdd(ws, 0);

        try
        {
            var buffer = new byte[1024];
            while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (WebSocketException) { }
        finally
        {
            try
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
            catch
            {
                // ignored
            }

            ws.Dispose();
            _connectedSockets.TryRemove(ws, out _);
            RemoveDeviceSocket(ws);
        }
    }

    private async Task ServeFile(NetworkStream stream, string path, CancellationToken ct)
    {
        if (_activeDashboard is null)
        {
            await WriteResponse(stream, 503, "Service Unavailable", "text/plain", "No active dashboard", ct);
            return;
        }

        var relativePath = path.TrimStart('/');

        if (string.IsNullOrEmpty(relativePath) || relativePath == "index.html")
        {
            var dashboardPath = Path.Combine(_activeDashboard.DirectoryPath, "dashboard.html");
            await ServeFileContent(stream, dashboardPath, ct);
            return;
        }

        if (relativePath == "ws")
        {
            await WriteResponse(stream, 400, "Bad Request", "text/plain", "Use WebSocket upgrade", ct);
            return;
        }

        var fullPath = Path.Combine(_activeDashboard.DirectoryPath, relativePath);
        await ServeFileContent(stream, fullPath, ct);
    }

    private async Task ServeFileContent(NetworkStream stream, string fullPath, CancellationToken ct)
    {
        var fullFile = Path.GetFullPath(fullPath);
        var dashboardDir = Path.GetFullPath(_activeDashboard!.DirectoryPath);

        if (!fullFile.StartsWith(dashboardDir, StringComparison.Ordinal))
        {
            await WriteResponse(stream, 403, "Forbidden", "text/plain", "Forbidden", ct);
            return;
        }

        if (!File.Exists(fullFile))
        {
            await WriteResponse(stream, 404, "Not Found", "text/plain", "File not found", ct);
            return;
        }

        var ext = Path.GetExtension(fullFile).ToLowerInvariant();
        var contentType = ext switch
        {
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".json" => "application/json",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            ".woff2" => "font/woff2",
            ".woff" => "font/woff",
            _ => "application/octet-stream"
        };

        var content = await File.ReadAllBytesAsync(fullFile, ct);
        var header = $"HTTP/1.1 200 OK\r\n" +
                     $"Content-Type: {contentType}\r\n" +
                     $"Content-Length: {content.Length}\r\n" +
                     $"Cache-Control: no-cache\r\n" +
                     $"Access-Control-Allow-Origin: *\r\n" +
                     $"Connection: close\r\n\r\n";

        var headerBytes = Encoding.UTF8.GetBytes(header);
        await stream.WriteAsync(headerBytes, 0, headerBytes.Length, ct);
        await stream.WriteAsync(content, 0, content.Length, ct);
        await stream.FlushAsync(ct);
    }

    private static async Task WriteResponse(NetworkStream stream, int statusCode, string statusText,
        string contentType, string body, CancellationToken ct)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var header = $"HTTP/1.1 {statusCode} {statusText}\r\n" +
                     $"Content-Type: {contentType}\r\n" +
                     $"Content-Length: {bodyBytes.Length}\r\n" +
                     $"Access-Control-Allow-Origin: *\r\n" +
                     $"Connection: close\r\n\r\n";
        var headerBytes = Encoding.UTF8.GetBytes(header);
        await stream.WriteAsync(headerBytes, 0, headerBytes.Length, ct);
        await stream.WriteAsync(bodyBytes, 0, bodyBytes.Length, ct);
        await stream.FlushAsync(ct);
    }

    private static string ComputeWebSocketAcceptKey(string key)
    {
        var combined = key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(combined));
        return Convert.ToBase64String(hash);
    }

    private static int FindFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string GetLocalIpAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ip = host.AddressList.FirstOrDefault(addr =>
                addr.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(addr));
            return ip?.ToString() ?? "127.0.0.1";
        }
        catch
        {
            return "127.0.0.1";
        }
    }
}
