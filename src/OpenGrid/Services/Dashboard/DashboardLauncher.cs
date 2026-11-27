using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
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

    // One dashboard window per screen. Each window keeps a stack of dashboard states so
    // temporary game overrides can be undone when the game that triggered them stops.
    private readonly Dictionary<string, OpenDashboardWindow> _openWindows = new(StringComparer.Ordinal);

    private SteamGame? _currentGame;
    private string? _currentCarName;

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
        _telemetryService.TelemetryReceived += OnTelemetryReceived;
        _telemetryService.TelemetryStatusChanged += OnTelemetryStatusChanged;
    }

    public void HandleTrigger(DashboardLaunchTrigger trigger)
    {
        if (trigger == DashboardLaunchTrigger.None)
            return;

        var devices = _deviceService.GetPersistedDevices();
        foreach (var device in devices)
        {
            if (device is not DisplayDevice { IsEnabled: true } displayDevice)
                continue;

            if (displayDevice.DashboardTrigger != trigger)
                continue;

            var dashboard = ResolveDashboard(displayDevice, null, out _);
            if (dashboard is null)
                continue;

            OpenDashboardForDevice(displayDevice, trigger, dashboard.Id);
        }
    }

    public bool IsDeviceDashboardOpen(DisplayDevice displayDevice)
    {
        return _openWindows.ContainsKey(GetScreenKey(displayDevice));
    }

    /// <summary>Re-resolves an open window after its default/overrides changed.</summary>
    public void RefreshDeviceDashboard(DisplayDevice displayDevice)
    {
        ApplyResolvedDashboard(displayDevice);
    }

    public void OpenDeviceDashboard(DisplayDevice displayDevice)
    {
        var dashboard = ResolveDashboard(displayDevice, null, out var matchingOverride);
        if (dashboard is null)
            return;

        var trigger = matchingOverride is not null
            ? GetOverrideTrigger(matchingOverride)
            : DashboardLaunchTrigger.None;

        OpenDashboardForDevice(
            displayDevice,
            trigger,
            dashboard.Id,
            matchingOverride?.GameAppId);
    }

    public void CloseDeviceDashboard(DisplayDevice displayDevice)
    {
        RunOnUiThread(() => CloseWindow(GetScreenKey(displayDevice)));
    }

    private void CloseForTrigger(DashboardLaunchTrigger trigger)
    {
        RunOnUiThread(() => CloseForTriggerCore(trigger));
    }

    private void CloseForTriggerCore(DashboardLaunchTrigger trigger)
    {
        foreach (var entry in _openWindows.Values.ToList())
        {
            var changed = entry.Window.RemoveDashboardsForTrigger(trigger);

            if (entry.Window.DashboardCount == 0)
            {
                CloseWindow(entry);
                continue;
            }

            if (changed)
                NotifyStateChanged(entry);
        }
    }

    private void OnGameStarted(object? sender, SteamGameProcess e)
    {
        _currentGame = e.SteamGame;
        HandleGameStart(e.SteamGame.AppId);
    }

    private void HandleGameStart(int appId)
    {
        var devices = _deviceService.GetPersistedDevices();
        foreach (var device in devices)
        {
            if (device is not DisplayDevice { IsEnabled: true } displayDevice)
                continue;

            var dashboard = ResolveDashboard(displayDevice, appId, out var matchingOverride);
            if (dashboard is null)
                continue;

            var isOverride = matchingOverride is not null;
            var trigger = isOverride
                ? GetOverrideTrigger(matchingOverride!)
                : displayDevice.DashboardTrigger;

            OpenDashboardForDevice(
                displayDevice,
                trigger,
                dashboard.Id,
                isOverride ? matchingOverride!.GameAppId : appId,
                replaceExisting: isOverride);
        }
    }

    private void OnGameStopped(object? sender, SteamGameProcess e)
    {
        RestoreForGame(e.SteamGame.AppId);
    }

    private void RestoreForGame(int appId)
    {
        RunOnUiThread(() => RestoreForGameCore(appId));
    }

    private void RestoreForGameCore(int appId)
    {
        foreach (var entry in _openWindows.Values.ToList())
        {
            var changed = entry.Window.RemoveDashboardsForGame(appId);

            if (entry.Window.DashboardCount == 0)
            {
                CloseWindow(entry);
                continue;
            }

            if (changed)
                NotifyStateChanged(entry);
        }
    }

    private void OnTelemetryReceived(object? sender, TelemetryEventArgs e)
    {
        var gameChanged = _currentGame?.AppId != e.Game.AppId;
        var carChanged = !string.Equals(
            _currentCarName,
            e.Telemetry.Car.Name,
            StringComparison.OrdinalIgnoreCase);

        if (!gameChanged && !carChanged)
            return;

        _currentGame = e.Game;
        _currentCarName = e.Telemetry.Car.Name;

        ReapplyDashboardOverrides();
    }

    private void OnTelemetryStatusChanged(object? sender, TelemetryConnectionStatus e)
    {
        if (e == TelemetryConnectionStatus.Connected)
        {
            HandleTrigger(DashboardLaunchTrigger.OnTelemetryConnected);
        }
        else if (e == TelemetryConnectionStatus.Disconnected)
        {
            _currentGame = null;
            _currentCarName = null;
            CloseForTrigger(DashboardLaunchTrigger.OnTelemetryConnected);
        }
    }

    /// <summary>
    /// Applies the same game/car specificity rules as bass shaker and LED presets.
    /// </summary>
    private DashboardInfo? ResolveDashboard(
        DisplayDevice displayDevice,
        int? gameAppId,
        out DashboardPresetOverride? matchingOverride)
    {
        gameAppId ??= _currentGame?.AppId;

        matchingOverride = displayDevice.DashboardOverrides
            .Where(x => x.Matches(gameAppId, _currentCarName))
            .OrderByDescending(x => x.GetSpecificity())
            .FirstOrDefault(x => GetDashboardById(x.PresetId) is not null);

        return matchingOverride is not null
            ? GetDashboardById(matchingOverride.PresetId)
            : GetDashboardById(displayDevice.DashboardId);
    }

    private static DashboardLaunchTrigger GetOverrideTrigger(DashboardPresetOverride matchingOverride)
    {
        return matchingOverride.GameAppId is not null
            ? DashboardLaunchTrigger.OnGameStart
            : DashboardLaunchTrigger.OnTelemetryConnected;
    }

    /// <summary>
    /// Switches open dashboards when the current game or car starts matching a
    /// different override, just like presets are reconfigured on telemetry changes.
    /// </summary>
    private void ReapplyDashboardOverrides()
    {
        foreach (var device in _deviceService.GetPersistedDevices())
        {
            if (device is DisplayDevice { IsEnabled: true } displayDevice)
            {
                ApplyResolvedDashboard(displayDevice);
            }
        }
    }

    private void ApplyResolvedDashboard(DisplayDevice displayDevice)
    {
        if (!_openWindows.TryGetValue(GetScreenKey(displayDevice), out var entry))
            return;

        var dashboard = ResolveDashboard(displayDevice, null, out var matchingOverride);
        if (dashboard is null)
        {
            CloseWindow(entry);
            return;
        }

        if (string.Equals(
                entry.Window.CurrentDashboard?.DashboardId,
                dashboard.Id,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var trigger = matchingOverride is not null
            ? GetOverrideTrigger(matchingOverride)
            : DashboardLaunchTrigger.None;

        OpenDashboardForDevice(
            displayDevice,
            trigger,
            dashboard.Id,
            matchingOverride?.GameAppId,
            replaceExisting: true);
    }

    private DashboardInfo? GetDashboardById(string? dashboardId)
    {
        return string.IsNullOrWhiteSpace(dashboardId)
            ? null
            : _dashboardService.GetById(dashboardId);
    }

    private void OpenDashboardForDevice(
        DisplayDevice displayDevice,
        DashboardLaunchTrigger trigger,
        string? dashboardId = null,
        int? gameAppId = null,
        bool replaceExisting = false)
    {
        dashboardId ??= displayDevice.DashboardId;

        if (dashboardId is null)
            return;

        var dashboard = _dashboardService.GetById(dashboardId);
        if (dashboard is null)
        {
            Log.Warning("Dashboard {DashboardId} not found for device {DeviceId}", dashboardId, displayDevice.Id);
            return;
        }

        var screen = displayDevice.Screen;
        if (screen is null)
        {
            Log.Warning("Screen not available for device {DeviceId}", displayDevice.Id);
            return;
        }

        var screenKey = GetScreenKey(displayDevice);
        var deviceId = displayDevice.Id;
        var state = new DashboardWindowState(
            dashboard.Id,
            GetDashboardUrl(dashboard),
            dashboard.Name,
            dashboard.Width,
            dashboard.Height,
            trigger,
            gameAppId);

        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                if (_openWindows.TryGetValue(screenKey, out var existing))
                {
                    existing.DeviceIds.Add(deviceId);

                    if (!replaceExisting)
                    {
                        DashboardStateChanged?.Invoke(this, deviceId);
                        return;
                    }

                    if (existing.Window.CurrentDashboard != state)
                        existing.Window.PushDashboard(state);

                    DashboardStateChanged?.Invoke(this, deviceId);
                    return;
                }

                var window = new DashboardWindow(state);
                ApplyWindowPlacement(window, screen);

                var entry = new OpenDashboardWindow(window, screenKey);
                entry.DeviceIds.Add(deviceId);
                window.Closed += (_, _) => OnWindowClosed(entry);

                _openWindows[screenKey] = entry;
                window.Show();
                NotifyStateChanged(entry);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to open dashboard {DashboardId} on device {DeviceId}", dashboard.Id, deviceId);
            }
        });
    }

    private void OnWindowClosed(OpenDashboardWindow entry)
    {
        if (!_openWindows.TryGetValue(entry.ScreenKey, out var current) || !ReferenceEquals(current, entry))
            return;

        _openWindows.Remove(entry.ScreenKey);
        NotifyStateChanged(entry);
    }

    private void CloseWindow(string screenKey)
    {
        if (_openWindows.TryGetValue(screenKey, out var entry))
            CloseWindow(entry);
    }

    private void CloseWindow(OpenDashboardWindow? entry)
    {
        if (entry is null)
            return;

        if (_openWindows.TryGetValue(entry.ScreenKey, out var current) && ReferenceEquals(current, entry))
            _openWindows.Remove(entry.ScreenKey);

        try
        {
            entry.Window.Close();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error closing dashboard window for screen {ScreenKey}", entry.ScreenKey);
        }

        NotifyStateChanged(entry);
    }

    private void NotifyStateChanged(OpenDashboardWindow entry)
    {
        foreach (var deviceId in entry.DeviceIds)
            DashboardStateChanged?.Invoke(this, deviceId);
    }

    private static string GetScreenKey(DisplayDevice displayDevice)
    {
        return displayDevice.Screen?.DisplayName ?? displayDevice.Id;
    }

    private string GetDashboardUrl(DashboardInfo dashboard)
    {
        return string.IsNullOrEmpty(dashboard.Url)
            ? $"http://localhost:{_dashboardServer.Port}/dashboards/{dashboard.Id}/"
            : dashboard.Url;
    }

    private static void ApplyWindowPlacement(DashboardWindow window, Screen screen)
    {
        var bounds = screen.Bounds;
        window.Position = new PixelPoint(bounds.X, bounds.Y);
        window.Width = bounds.Width;
        window.Height = bounds.Height;
        window.WindowState = WindowState.FullScreen;
        window.WindowDecorations = WindowDecorations.None;
        window.Topmost = true;
    }

    private static void RunOnUiThread(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
            action();
        else
            Dispatcher.UIThread.Post(action);
    }

    private sealed class OpenDashboardWindow
    {
        public OpenDashboardWindow(DashboardWindow window, string screenKey)
        {
            Window = window;
            ScreenKey = screenKey;
        }

        public DashboardWindow Window { get; }
        public string ScreenKey { get; }
        public HashSet<string> DeviceIds { get; } = new(StringComparer.Ordinal);
    }
}
