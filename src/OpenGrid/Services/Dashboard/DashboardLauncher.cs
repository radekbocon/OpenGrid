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

public sealed class DashboardLauncher
{
    private readonly IDeviceService _deviceService;
    private readonly IDashboardService _dashboardService;
    private readonly DashboardHttpServer _dashboardServer;
    private readonly SteamGameManager _steamGameManager;
    private readonly ITelemetryService _telemetryService;

    private readonly Dictionary<string, (DashboardWindow Window, DashboardLaunchTrigger Trigger)> _openWindows = new(StringComparer.Ordinal);

    public event EventHandler<string>? DashboardStateChanged;

    public DashboardLauncher(
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

        var devices = _deviceService.GetPersistedDevices();
        foreach (var device in devices)
        {
            if (device is not DisplayDevice { IsEnabled: true, DashboardId: not null } displayDevice)
                continue;

            if (displayDevice.DashboardTrigger != trigger)
                continue;
            
            OpenDashboardForDevice(displayDevice, trigger);
        }
    }

    public bool IsDeviceDashboardOpen(string deviceId)
    {
        return _openWindows.ContainsKey(deviceId);
    }

    public void OpenDeviceDashboard(DisplayDevice displayDevice)
    {
        if (displayDevice.DashboardId is null)
            return;

        OpenDashboardForDevice(displayDevice, DashboardLaunchTrigger.None);
    }

    public void CloseDeviceDashboard(string deviceId)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            CloseDeviceDashboardCore(deviceId);
        }
        else
        {
            Dispatcher.UIThread.Post(() => CloseDeviceDashboardCore(deviceId));
        }
    }

    private void CloseDeviceDashboardCore(string deviceId)
    {
        if (!_openWindows.TryGetValue(deviceId, out var entry))
            return;

        try
        {
            entry.Window.Closed -= null!;
            entry.Window.Close();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error closing dashboard window for device {DeviceId}", deviceId);
        }
        _openWindows.Remove(deviceId);
        DashboardStateChanged?.Invoke(this, deviceId);
    }

    public void CloseForTrigger(DashboardLaunchTrigger trigger)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            CloseForTriggerCore(trigger);
        }
        else
        {
            Dispatcher.UIThread.Post(() => CloseForTriggerCore(trigger));
        }
    }

    private void CloseForTriggerCore(DashboardLaunchTrigger trigger)
    {
        var toClose = _openWindows.Where(kv => kv.Value.Trigger == trigger).ToList();
        foreach (var (id, (window, _)) in toClose)
        {
            try
            {
                window.Closed -= null!;
                window.Close();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error closing dashboard window");
            }
            _openWindows.Remove(id);
            DashboardStateChanged?.Invoke(this, id);
        }
    }

    private void OnGameStarted(object? sender, SteamGameProcess e)
    {
        HandleTrigger(DashboardLaunchTrigger.OnGameStart);
    }

    private void OnGameStopped(object? sender, SteamGameProcess e)
    {
        CloseForTrigger(DashboardLaunchTrigger.OnGameStart);
    }

    private void OnTelemetryStatusChanged(object? sender, TelemetryConnectionStatus e)
    {
        if (e == TelemetryConnectionStatus.Connected)
        {
            HandleTrigger(DashboardLaunchTrigger.OnTelemetryConnected);
        }
        else if (e == TelemetryConnectionStatus.Disconnected)
        {
            CloseForTrigger(DashboardLaunchTrigger.OnTelemetryConnected);
        }
    }

    private void OpenDashboardForDevice(DisplayDevice displayDevice, DashboardLaunchTrigger trigger)
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
                    DashboardStateChanged?.Invoke(this, displayDevice.Id);
                };

                _openWindows[displayDevice.Id] = (window, trigger);
                window.Show();
                DashboardStateChanged?.Invoke(this, displayDevice.Id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to open dashboard {DashboardId} on device {DeviceId}", displayDevice.DashboardId, displayDevice.Id);
            }
        });
    }
}
