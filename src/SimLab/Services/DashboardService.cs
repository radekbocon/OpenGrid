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
    private readonly IDashboardRepository _repository;
    private readonly ITelemetryService _telemetryService;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptLoop;
    private readonly ConcurrentDictionary<WebSocket, byte> _connectedSockets = [];
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<WebSocket, byte>> _deviceSockets = new(StringComparer.OrdinalIgnoreCase);

    public bool IsRunning => _listener is not null;
    public int Port { get; private set; }

    private string LocalUrl => $"http://127.0.0.1:{Port}";
    private string NetworkUrl => $"http://{GetLocalIpAddress()}:{Port}";

    public DashboardService(IDashboardRepository repository, ITelemetryService telemetryService)
    {
        _repository = repository;
        _telemetryService = telemetryService;
    }

    public string GetDashboardUrl(DashboardInfo dashboard, bool useNetwork)
    {
        EnsureRunning();
        var baseUrl = useNetwork ? NetworkUrl : LocalUrl;
        return $"{baseUrl}/d/{dashboard.Id}/";
    }

    public void OpenInBrowser(DashboardInfo dashboard)
    {
        var url = GetDashboardUrl(dashboard, useNetwork: false);
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

    public void OpenInWebView(DashboardInfo dashboard)
    {
        OpenInBrowser(dashboard);
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
            try { kvp.Key.Dispose(); } catch { }
        }
        _connectedSockets.Clear();
        _deviceSockets.Clear();

        Log.Information("Dashboard HTTP server stopped");
    }

    private void EnsureRunning()
    {
        if (IsRunning) return;

        Port = FindFreePort();
        _listener = new TcpListener(IPAddress.Any, Port);
        _listener.Start();
        _cts = new CancellationTokenSource();
        _acceptLoop = AcceptConnectionsAsync(_cts.Token);
        _telemetryService.TelemetryReceived += OnTelemetryReceived;
        Log.Information("Dashboard HTTP server started on port {Port}", Port);
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
                gear = (int)e.Telemetry.CurrentGear - 1,
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
                tireTemps = new
                {
                    fl = e.Telemetry.TireTemperatures.FrontLeft,
                    fr = e.Telemetry.TireTemperatures.FrontRight,
                    rl = e.Telemetry.TireTemperatures.RearLeft,
                    rr = e.Telemetry.TireTemperatures.RearRight
                }
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
            try { dead.Dispose(); } catch { }
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
                await ServeFile(stream, path, ct, remoteIp);
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
            try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None); } catch { }
            ws.Dispose();
            _connectedSockets.TryRemove(ws, out _);
            RemoveDeviceSocket(ws);
        }
    }

    private async Task ServeFile(NetworkStream stream, string path, CancellationToken ct, string remoteIp)
    {
        var relativePath = path.TrimStart('/');

        if (relativePath == "")
        {
            await WriteResponse(stream, 200, "OK", "text/html", GetDashboardListHtml(remoteIp), ct);
            return;
        }

        if (!relativePath.StartsWith("d/"))
        {
            await WriteResponse(stream, 404, "Not Found", "text/plain", "Not Found", ct);
            return;
        }

        var afterD = relativePath[2..].TrimStart('/');
        var slashIdx = afterD.IndexOf('/');
        var dashboardId = slashIdx > 0 ? afterD[..slashIdx] : afterD;
        var filePath = slashIdx > 0 ? afterD[(slashIdx + 1)..] : "";

        var dashboard = _repository.GetById(dashboardId);
        if (dashboard is null)
        {
            await WriteResponse(stream, 404, "Not Found", "text/plain", $"Dashboard '{dashboardId}' not found", ct);
            return;
        }

        var dir = dashboard.DirectoryPath;
        var fullPath = string.IsNullOrEmpty(filePath) || filePath == "index.html"
            ? Path.Combine(dir, "dashboard.html")
            : Path.Combine(dir, filePath);

        var fullDir = Path.GetFullPath(dir);
        var fullFile = Path.GetFullPath(fullPath);
        if (!fullFile.StartsWith(fullDir, StringComparison.Ordinal))
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

    private string GetDashboardListHtml(string remoteIp)
    {
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset=\"UTF-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">");
        sb.Append("<title>SimLab Dashboards</title>");
        sb.Append("<style>");
        sb.Append("body{font-family:sans-serif;background:#0a0a0f;color:#fff;padding:20px;max-width:600px;margin:0 auto;}");
        sb.Append("h1{font-size:22px;margin-bottom:20px;}");
        sb.Append("a{color:#00d4ff;text-decoration:none;display:block;padding:14px;margin:8px 0;background:#14141e;border-radius:10px;border:1px solid rgba(255,255,255,0.06);font-size:16px;}");
        sb.Append("a:hover{background:#1a1a28;border-color:#00d4ff40;}");
        sb.Append(".note{color:#666;font-size:13px;margin-top:24px;text-align:center;}");
        sb.Append("</style>");
        sb.Append("</head><body>");
        sb.Append("<h1>SimLab Dashboards</h1>");

        foreach (var d in _repository.GetAllDashboards())
        {
            sb.Append($"<a href=\"/d/{d.Id}/\">{d.Name}</a>");
        }

        sb.Append("<p class=\"note\">Open a dashboard above. Telemetry data will stream automatically.</p>");
        sb.Append("</body></html>");
        return sb.ToString();
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
