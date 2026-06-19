using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using OpenGrid.Services.Telemetry;
using Serilog;

namespace OpenGrid.Services.Dashboard;

public sealed class DashboardHttpServer : IDisposable
{
    private static readonly Assembly Assembly = Assembly.GetExecutingAssembly();
    private static readonly string FrameworkExtractPath = Path.Combine(Program.AppDataDirectory, "dashboard-framework");

    private static readonly Dictionary<string, string> MimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".html"] = "text/html",
        [".htm"] = "text/html",
        [".js"] = "application/javascript",
        [".css"] = "text/css",
        [".json"] = "application/json",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".svg"] = "image/svg+xml",
        [".ico"] = "image/x-icon",
        [".woff2"] = "font/woff2",
        [".woff"] = "font/woff",
        [".ttf"] = "font/ttf",
        [".otf"] = "font/otf",
        [".map"] = "application/json",
    };

    private readonly IDashboardService _dashboardService;
    private readonly ITelemetryService _telemetryService;
    private readonly TelemetryWebSocketBroadcaster _wsBroadcaster;
    private readonly HttpListener _listener;

    private CancellationTokenSource? _cts;
    private Task? _listenTask;

    public int Port { get; }

    public bool IsRunning { get; private set; }

    public DashboardHttpServer(
        IDashboardService dashboardService,
        ITelemetryService telemetryService,
        TelemetryWebSocketBroadcaster wsBroadcaster)
    {
        _dashboardService = dashboardService;
        _telemetryService = telemetryService;
        _wsBroadcaster = wsBroadcaster;
        Port = 8190;

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{Port}/");
        _listener.Prefixes.Add($"http://*:{Port}/");

        ExtractFramework();
        UpdateDashboardUrls();
    }

    public Task StartAsync()
    {
        if (IsRunning)
            return Task.CompletedTask;

        try
        {
            _listener.Start();
            IsRunning = true;

            _telemetryService.TelemetryReceived += OnTelemetryReceived;
            _wsBroadcaster.Start();

            _cts = new CancellationTokenSource();
            _listenTask = ListenLoopAsync(_cts.Token);

            UpdateDashboardUrls();

            Log.Information("Dashboard HTTP server started on port {Port}", Port);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to start dashboard HTTP server on port {Port}", Port);
            IsRunning = false;
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (!IsRunning)
            return;

        _telemetryService.TelemetryReceived -= OnTelemetryReceived;
        _wsBroadcaster.Stop();

        _cts?.Cancel();

        try
        {
            _listener.Stop();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error stopping HTTP listener");
        }

        if (_listenTask is not null)
        {
            try
            {
                await _listenTask;
            }
            catch (OperationCanceledException)
            {
                // expected
            }
        }

        _cts?.Dispose();
        _cts = null;
        IsRunning = false;

        Log.Information("Dashboard HTTP server stopped");
    }

    public void Dispose()
    {
        _ = StopAsync();
        (_listener as IDisposable).Dispose();
    }

    private void OnTelemetryReceived(object? sender, TelemetryEventArgs e)
    {
        _wsBroadcaster.OnTelemetryReceived(e.Telemetry);
    }

    private void UpdateDashboardUrls()
    {
        foreach (var dashboard in _dashboardService.Dashboards)
        {
            dashboard.Url = $"http://localhost:{Port}/dashboards/{dashboard.Id}/";
        }
    }

    private async Task ListenLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var ctx = await _listener.GetContextAsync().WaitAsync(ct);
                _ = HandleRequestAsync(ctx, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 995)
            {
                // Listener stopped
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in HTTP listener loop");
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        try
        {
            var path = ctx.Request.Url?.AbsolutePath.Trim('/') ?? "";

            if (ctx.Request.IsWebSocketRequest && string.Equals(path, "ws/telemetry", StringComparison.OrdinalIgnoreCase))
            {
                await HandleWebSocketUpgrade(ctx);
                return;
            }

            if (string.IsNullOrEmpty(path))
            {
                await WriteStringResponse(ctx.Response, 200, "OpenGrid Dashboard Server is running", "text/plain");
                return;
            }

            if (string.Equals(path, "api/dashboards", StringComparison.OrdinalIgnoreCase))
            {
                await ServeApiDashboards(ctx.Response);
                return;
            }

            if (string.Equals(path, "api/telemetry/latest", StringComparison.OrdinalIgnoreCase))
            {
                await ServeApiTelemetry(ctx.Response);
                return;
            }

            if (path.StartsWith("framework/", StringComparison.OrdinalIgnoreCase))
            {
                await ServeStaticFile(ctx.Response, FrameworkExtractPath, path["framework/".Length..], ct);
                return;
            }

            if (path.StartsWith("dashboards/", StringComparison.OrdinalIgnoreCase))
            {
                await ServeDashboardFile(ctx.Response, path["dashboards/".Length..], ct);
                return;
            }

            await WriteStringResponse(ctx.Response, 404, "Not Found", "text/plain");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error handling HTTP request: {Path}", ctx.Request.Url?.AbsolutePath);
            try
            {
                await WriteStringResponse(ctx.Response, 500, "Internal Server Error", "text/plain");
            }
            catch
            {
                // ignore
            }
        }
    }

    private async Task HandleWebSocketUpgrade(HttpListenerContext ctx)
    {
        try
        {
            var wsContext = await ctx.AcceptWebSocketAsync(null);
            var webSocket = wsContext.WebSocket;
            await _wsBroadcaster.HandleConnectionAsync(webSocket);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "WebSocket upgrade failed");
            ctx.Response.StatusCode = 500;
            ctx.Response.Close();
        }
    }

    private async Task ServeApiDashboards(HttpListenerResponse res)
    {
        var dashboards = _dashboardService.Dashboards.Select(d => new
        {
            id = d.Id,
            name = d.Name,
            description = d.Description,
            type = d.Type.ToString().ToLowerInvariant(),
            isSystem = d.IsSystem,
            url = d.Url
        });

        var json = JsonSerializer.Serialize(dashboards, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await WriteStringResponse(res, 200, json, "application/json");
    }

    private async Task ServeApiTelemetry(HttpListenerResponse res)
    {
        var json = "null";

        // Try to get the latest snapshot from the last broadcast
        // HttpListener is response-based, so just return null for now
        // Real-time data goes through WebSocket
        await WriteStringResponse(res, 200, json, "application/json");
    }

    private async Task ServeDashboardFile(HttpListenerResponse res, string relativePath, CancellationToken ct)
    {
        // relativePath format: "{dashboardId}/..." or "{dashboardId}"
        var slashIndex = relativePath.IndexOf('/');
        string dashboardId;
        string filePath;

        if (slashIndex < 0)
        {
            dashboardId = relativePath;
            filePath = "index.html";
        }
        else
        {
            dashboardId = relativePath[..slashIndex];
            filePath = relativePath[(slashIndex + 1)..];
        }

        var dashboard = _dashboardService.GetById(dashboardId);
        if (dashboard is null)
        {
            await WriteStringResponse(res, 404, $"Dashboard '{dashboardId}' not found", "text/plain");
            return;
        }

        if (string.IsNullOrEmpty(filePath) || filePath.EndsWith('/'))
        {
            filePath = "index.html";
        }

        await ServeStaticFile(res, dashboard.DirectoryPath, filePath, ct);
    }

    private static async Task ServeStaticFile(HttpListenerResponse res, string basePath, string relativePath,
        CancellationToken ct)
    {
        // Security: prevent path traversal
        var fullPath = Path.GetFullPath(Path.Combine(basePath, relativePath));
        if (!fullPath.StartsWith(Path.GetFullPath(basePath), StringComparison.Ordinal))
        {
            await WriteStringResponse(res, 403, "Forbidden", "text/plain");
            return;
        }

        if (!File.Exists(fullPath))
        {
            await WriteStringResponse(res, 404, "File Not Found", "text/plain");
            return;
        }

        try
        {
            var extension = Path.GetExtension(fullPath);
            var contentType = MimeTypes.GetValueOrDefault(extension, "application/octet-stream");

            res.ContentType = contentType;
            res.StatusCode = 200;

            await using var fileStream = File.OpenRead(fullPath);
            await fileStream.CopyToAsync(res.OutputStream, ct);
        }
        catch (OperationCanceledException)
        {
            // client disconnected
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error serving static file: {Path}", fullPath);
            try
            {
                await WriteStringResponse(res, 500, "Error reading file", "text/plain");
            }
            catch
            {
                // ignore
            }
        }
        finally
        {
            try
            {
                res.Close();
            }
            catch
            {
                // ignore
            }
        }
    }

    private static async Task WriteStringResponse(HttpListenerResponse res, int statusCode, string body,
        string contentType)
    {
        try
        {
            res.StatusCode = statusCode;
            res.ContentType = contentType;
            var bytes = Encoding.UTF8.GetBytes(body);
            res.ContentLength64 = bytes.Length;
            await res.OutputStream.WriteAsync(bytes);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error writing HTTP response");
        }
        finally
        {
            try
            {
                res.Close();
            }
            catch
            {
                // ignore
            }
        }
    }

    private static void ExtractFramework()
    {
        var resourcePrefix = "OpenGrid.Assets.DashboardFramework.";
        var resources = Assembly.GetManifestResourceNames()
            .Where(r => r.StartsWith(resourcePrefix, StringComparison.OrdinalIgnoreCase));

        foreach (var resourceName in resources)
        {
            var relativePath = resourceName[resourcePrefix.Length..];
            var filePath = Path.Combine(FrameworkExtractPath, relativePath);

            var dir = Path.GetDirectoryName(filePath);
            if (dir is not null)
                Directory.CreateDirectory(dir);

            if (File.Exists(filePath))
                continue;

            using var stream = Assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
                continue;

            using var fileStream = File.Create(filePath);
            stream.CopyTo(fileStream);
        }
    }
}