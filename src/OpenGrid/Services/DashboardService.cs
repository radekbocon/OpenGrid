using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using OpenGrid.Models;
using OpenGrid.Services.Telemetry;

namespace OpenGrid.Services;

public class DashboardService : IDashboardService
{
    private static string? _frameworkTemplate;
    
    private readonly ITelemetryService _telemetryService;
    private readonly ISettingsService _settingsService;
    private readonly IDashboardRepository _repository;
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private readonly ConcurrentDictionary<WebSocket, byte> _connectedSockets = [];

    public bool IsRunning => _listener is { IsListening: true};
    public int Port { get; private set; }
    public DashboardInfo? ActiveDashboard { get; private set; }

    public event EventHandler<bool>? IsRunningChanged;

    private string LocalUrl => $"http://127.0.0.1:{Port}";
    private string NetworkUrl => $"http://{GetLocalIpAddress()}:{Port}";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public DashboardService(ITelemetryService telemetryService, ISettingsService settingsService, IDashboardRepository repository)
    {
        _telemetryService = telemetryService;
        _settingsService = settingsService;
        _repository = repository;
    }

    public void Start(DashboardInfo dashboard)
    {
        ActiveDashboard = dashboard;
        EnsureRunning();
    }

    public string GetUrl(bool useNetwork) => useNetwork ? NetworkUrl : LocalUrl;

    public void OpenInBrowser()
    {
        if (ActiveDashboard is null) return;
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

    public void Stop()
    {
        if (!IsRunning) return;

        _telemetryService.TelemetryReceived -= OnTelemetryReceived;
        _cts?.Cancel();
        _listener?.Stop();
        _listener?.Close();
        _listener = null;
        IsRunningChanged?.Invoke(this, false);

        foreach (var kvp in _connectedSockets)
        {
            try
            {
                kvp.Key.Dispose();
            }
            catch
            {
                // ignore
            }
        }
        _connectedSockets.Clear();

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

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://*:{Port}/");
        _listener.Start();
        _settingsService.DashboardPort = Port;
        _cts = new CancellationTokenSource();
        _ = AcceptConnectionsAsync(_cts.Token);
        _telemetryService.TelemetryReceived += OnTelemetryReceived;
        IsRunningChanged?.Invoke(this, true);
        Log.Information("Dashboard HTTP server started on port {Port}", Port);
    }

    private static bool IsPortAvailable(int port)
    {
        try
        {
            using var listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
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
            data = e.Telemetry
        }, JsonOptions);

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
            try
            {
                dead.Dispose();
            }
            catch
            {
                // ignore
            }
        }
    }

    private async Task AcceptConnectionsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var context = await _listener!.GetContextAsync().WaitAsync(ct);
                _ = HandleConnectionAsync(context, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (HttpListenerException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error accepting dashboard connection");
            }
        }
    }

    private async Task HandleConnectionAsync(HttpListenerContext context, CancellationToken ct)
    {
        var remoteIp = context.Request.RemoteEndPoint.Address.ToString();
        try
        {
            var request = context.Request;
            var path = request.Url?.AbsolutePath ?? "/";

            if (request.IsWebSocketRequest)
            {
                await HandleWebSocketUpgrade(context, ct, remoteIp);
                return;
            }

            if (request.HttpMethod == "GET")
            {
                var queryIdx = path.IndexOf('?');
                var pathOnly = queryIdx >= 0 ? path[..queryIdx] : path;

                switch (pathOnly)
                {
                    case "/api/dashboards":
                        await ServeDashboardList(context, ct);
                        break;
                    case not null when pathOnly.StartsWith("/api/dashboard/"):
                        await ServeDashboardApi(context, pathOnly, ct);
                        break;
                    default:
                        await ServeFile(context, pathOnly!, ct);
                        break;
                }
            }
            else
            {
                context.Response.StatusCode = 405;
                context.Response.Close();
            }
        }
        catch (OperationCanceledException) { }
        catch (HttpListenerException) { }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error handling dashboard connection from {Ip}", remoteIp);
        }
    }

    private async Task HandleWebSocketUpgrade(HttpListenerContext context, CancellationToken ct, string remoteIp)
    {
        WebSocket ws;
        try
        {
            var wsContext = await context.AcceptWebSocketAsync(subProtocol: null);
            ws = wsContext.WebSocket;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "WebSocket upgrade failed from {Ip}", remoteIp);
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }

        _connectedSockets.TryAdd(ws, 0);

        try
        {
            var buffer = new byte[1024];
            while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                if (result.MessageType == WebSocketMessageType.Close)
                    break;
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
                // ignore
            }
            ws.Dispose();
            _connectedSockets.TryRemove(ws, out _);
        }
    }

    private async Task ServeDashboardList(HttpListenerContext context, CancellationToken ct)
    {
        var dashboards = _repository.GetAllDashboards();
        var list = dashboards.Select(d => new
        {
            id = d.Id,
            name = d.Name,
            description = d.Description,
            image = File.Exists(Path.Combine(d.DirectoryPath, "image.png"))
                ? $"/api/dashboard/{d.Id}/image"
                : null
        }).ToList();

        var json = JsonSerializer.Serialize(list, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        var response = context.Response;
        response.StatusCode = 200;
        response.ContentType = "application/json; charset=utf-8";
        response.ContentLength64 = bytes.Length;
        response.Headers["Access-Control-Allow-Origin"] = "*";
        await response.OutputStream.WriteAsync(bytes, 0, bytes.Length, ct);
        response.Close();
    }

    private async Task ServeDashboardApi(HttpListenerContext context, string path, CancellationToken ct)
    {
        var segments = path.Split('/');
        if (segments.Length < 4)
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }

        var id = segments[3];
        var dashboard = _repository.GetById(id);
        if (dashboard is null)
        {
            context.Response.StatusCode = 404;
            context.Response.Close();
            return;
        }

        var action = segments.Length > 4 ? segments[4] : "";

        if (action == "html")
        {
            var htmlPath = Path.Combine(dashboard.DirectoryPath, "dashboard.html");
            if (!File.Exists(htmlPath))
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
                return;
            }

            var html = await File.ReadAllTextAsync(htmlPath, ct);
            var bodyStart = html.IndexOf("<body>", StringComparison.OrdinalIgnoreCase);
            var bodyEnd = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
            var bodyContent = bodyStart >= 0 && bodyEnd > bodyStart
                ? html[(bodyStart + 6)..bodyEnd]
                : html;

            var result = new
            {
                id = dashboard.Id,
                name = dashboard.Name,
                description = dashboard.Description,
                html = bodyContent
            };

            var json = JsonSerializer.Serialize(result, JsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);

            var response = context.Response;
            response.StatusCode = 200;
            response.ContentType = "application/json; charset=utf-8";
            response.ContentLength64 = bytes.Length;
            response.Headers["Access-Control-Allow-Origin"] = "*";
            await response.OutputStream.WriteAsync(bytes, 0, bytes.Length, ct);
            response.Close();
            return;
        }

        if (action == "image")
        {
            var imagePath = Path.Combine(dashboard.DirectoryPath, "image.png");
            if (!File.Exists(imagePath))
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
                return;
            }

            var content = await File.ReadAllBytesAsync(imagePath, ct);

            var response = context.Response;
            response.StatusCode = 200;
            response.ContentType = "image/png";
            response.ContentLength64 = content.Length;
            response.Headers["Cache-Control"] = "max-age=3600";
            response.Headers["Access-Control-Allow-Origin"] = "*";
            await response.OutputStream.WriteAsync(content, 0, content.Length, ct);
            response.Close();
            return;
        }

        context.Response.StatusCode = 400;
        context.Response.Close();
    }

    private async Task ServeFile(HttpListenerContext context, string path, CancellationToken ct)
    {
        var relativePath = path.TrimStart('/');

        if (string.IsNullOrEmpty(relativePath) || relativePath == "index.html")
        {
            await ServeFrameworkPage(context, ct);
            return;
        }

        if (relativePath == "ws")
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }

        if (ActiveDashboard is null)
        {
            context.Response.StatusCode = 503;
            context.Response.Close();
            return;
        }

        var fullPath = Path.Combine(ActiveDashboard.DirectoryPath, relativePath);
        await ServeFileContent(context, fullPath, ct);
    }

    private async Task ServeFrameworkPage(HttpListenerContext context, CancellationToken ct)
    {
        if (_frameworkTemplate is null)
        {
            var assembly = Assembly.GetExecutingAssembly();
            const string resourceName = "OpenGrid.Assets.DashboardFramework.index.html";
            await using var resStream = assembly.GetManifestResourceStream(resourceName);
            if (resStream is null)
            {
                context.Response.StatusCode = 500;
                context.Response.Close();
                return;
            }
            using var reader = new StreamReader(resStream);
            _frameworkTemplate = await reader.ReadToEndAsync(ct);
        }

        var pageContent = _frameworkTemplate;

        if (ActiveDashboard is not null)
        {
            var dashboardPath = Path.Combine(ActiveDashboard.DirectoryPath, "dashboard.html");
            if (File.Exists(dashboardPath))
            {
                var dashboardContent = await File.ReadAllTextAsync(dashboardPath, ct);
                var bodyStart = dashboardContent.IndexOf("<body>", StringComparison.OrdinalIgnoreCase);
                var bodyEnd = dashboardContent.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);

                if (bodyStart >= 0 && bodyEnd > bodyStart)
                {
                    var bodyContent = dashboardContent[(bodyStart + 6)..bodyEnd];
                    pageContent = pageContent.Replace("<!--DASHBOARD_CONTENT-->", bodyContent);
                }
            }
        }

        var bytes = Encoding.UTF8.GetBytes(pageContent);
        var response = context.Response;
        response.StatusCode = 200;
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = bytes.Length;
        response.Headers["Cache-Control"] = "no-cache";
        response.Headers["Access-Control-Allow-Origin"] = "*";
        await response.OutputStream.WriteAsync(bytes, 0, bytes.Length, ct);
        response.Close();
    }

    private async Task ServeFileContent(HttpListenerContext context, string fullPath, CancellationToken ct)
    {
        var fullFile = Path.GetFullPath(fullPath);
        var dashboardDir = Path.GetFullPath(ActiveDashboard!.DirectoryPath);

        if (!fullFile.StartsWith(dashboardDir, StringComparison.Ordinal))
        {
            context.Response.StatusCode = 403;
            context.Response.Close();
            return;
        }

        if (!File.Exists(fullFile))
        {
            context.Response.StatusCode = 404;
            context.Response.Close();
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

        var response = context.Response;
        response.StatusCode = 200;
        response.ContentType = contentType;
        response.ContentLength64 = content.Length;
        response.Headers["Cache-Control"] = "no-cache";
        response.Headers["Access-Control-Allow-Origin"] = "*";
        await response.OutputStream.WriteAsync(content, 0, content.Length, ct);
        response.Close();
    }

    private static int FindFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
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
