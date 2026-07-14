using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using OpenGrid.Models;
using OpenGrid.Services.Devices;
using OpenGrid.Services.Telemetry;
using OpenGrid.Views;
using Serilog;

namespace OpenGrid.Services.Dashboard;

public sealed class DashboardLaunchService : IDisposable
{
    private readonly IDeviceService _deviceService;
    private readonly IDashboardService _dashboardService;
    private readonly DashboardHttpServer _dashboardServer;
    private readonly SteamGameManager _steamGameManager;
    private readonly ITelemetryService _telemetryService;

    private readonly Dictionary<string, DashboardWindow> _openWindows = new(StringComparer.Ordinal);

    public DashboardLaunchService(
        IDeviceService deviceService,
        IDashboardService dashboardService,
        DashboardHttpServer dashboardServer,
        SteamGameManager steamGameManager,
        ITelemetryService telemetryService)
    {
        _deviceService = deviceService;
        _dashboardService = dashboardService;
        _dashboardServer = dashboardServer;
        _steamGameManager = steamGameManager;
        _telemetryService = telemetryService;

        _steamGameManager.GameStarted += OnGameStarted;
        _steamGameManager.GameStopped += OnGameStopped;
        _telemetryService.TelemetryStatusChanged += OnTelemetryStatusChanged;
    }

    public void HandleTrigger(DashboardLaunchTrigger trigger)
    {
        if (trigger == DashboardLaunchTrigger.None)
            return;

        var devices = _deviceService.GetSavedDevices();
        foreach (var device in devices)
        {
            if (device is not DisplayDevice displayDevice)
                continue;

            if (!displayDevice.IsEnabled)
                continue;

            if (displayDevice.DashboardTrigger != trigger)
                continue;

            if (string.IsNullOrEmpty(displayDevice.DashboardId))
                continue;

            OpenDashboardForDevice(displayDevice);
        }
    }

    public void CloseAll()
    {
        foreach (var window in _openWindows.Values)
        {
            try
            {
                window.Close();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error closing dashboard window");
            }
        }
        _openWindows.Clear();
    }

    public void Dispose()
    {
        _steamGameManager.GameStarted -= OnGameStarted;
        _steamGameManager.GameStopped -= OnGameStopped;
        _telemetryService.TelemetryStatusChanged -= OnTelemetryStatusChanged;
        CloseAll();
    }

    private void OnGameStarted(object? sender, SteamGameProcess e)
    {
        HandleTrigger(DashboardLaunchTrigger.OnGameStart);
    }

    private void OnGameStopped(object? sender, SteamGameProcess e)
    {
        CloseAll();
    }

    private void OnTelemetryStatusChanged(object? sender, TelemetryConnectionStatus e)
    {
        if (e == TelemetryConnectionStatus.Connected)
        {
            HandleTrigger(DashboardLaunchTrigger.OnTelemetryConnected);
        }
    }

    private void OpenDashboardForDevice(DisplayDevice displayDevice)
    {
        if (_openWindows.ContainsKey(displayDevice.Id))
            return;

        var dashboard = _dashboardService.GetById(displayDevice.DashboardId!);
        if (dashboard is null)
        {
            Log.Warning("Dashboard {DashboardId} not found for device {DeviceId}", displayDevice.DashboardId, displayDevice.Id);
            return;
        }

        if (displayDevice.Screen is null)
        {
            Log.Warning("Screen not available for device {DeviceId}", displayDevice.Id);
            return;
        }

        var url = $"http://localhost:{_dashboardServer.Port}/dashboards/{dashboard.Id}/";

        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                var window = new DashboardWindow(url, dashboard.Name, dashboard.Width, dashboard.Height);
                var bounds = displayDevice.Screen.Bounds;

                window.Position = new PixelPoint(bounds.X, bounds.Y);
                window.Width = bounds.Width;
                window.Height = bounds.Height;
                window.WindowState = WindowState.FullScreen;
                window.WindowDecorations = WindowDecorations.None;
                window.Topmost = true;

                window.Closed += (_, _) =>
                {
                    _openWindows.Remove(displayDevice.Id);
                };

                _openWindows[displayDevice.Id] = window;
                window.Show();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to open dashboard {DashboardId} on device {DeviceId}", displayDevice.DashboardId, displayDevice.Id);
            }
        });
    }
}
