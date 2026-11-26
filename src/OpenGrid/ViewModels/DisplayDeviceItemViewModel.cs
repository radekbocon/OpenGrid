using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using OpenGrid.Converters;
using OpenGrid.Models;
using OpenGrid.Services;
using OpenGrid.Services.Dashboard;
using OpenGrid.Services.Devices;

namespace OpenGrid.ViewModels;

public partial class DisplayDeviceItemViewModel : DeviceItemViewModel
{
    private readonly IDashboardService _dashboardService;
    private readonly DashboardLauncher _dashboardLauncher;

    public override MaterialIconKind IconKind => MaterialIconKind.Monitor;

    /// <summary>
    /// Same default + game/car override editor used by bass shakers and LEDs.
    /// </summary>
    public DevicePresetSettingsViewModel PresetSettings { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DashboardButtonLabel))]
    [NotifyCanExecuteChangedFor(nameof(ToggleDashboardCommand))]
    public partial bool IsDashboardOpen { get; set; }

    public string DashboardButtonLabel => IsDashboardOpen ? "Close Dashboard" : "Launch Dashboard";

    private bool CanToggleDashboard => PresetSettings.SelectedPresetId is not null;

    public DisplayDeviceItemViewModel(
        IDeviceService deviceService,
        IDashboardService dashboardService,
        DashboardLauncher dashboardLauncher,
        ICarConfigService carConfigService,
        Action<DeviceItemViewModel> onRemove)
        : base(deviceService, onRemove)
    {
        _dashboardService = dashboardService;
        _dashboardLauncher = dashboardLauncher;

        PresetSettings = new DevicePresetSettingsViewModel(
            carConfigService,
            static id => new DashboardPresetOverride { PresetId = id },
            OnPresetSettingsChanged,
            itemLabel: "Dashboard",
            itemsLocationHint: null,
            emptyOverridesHint: "No game or car overrides configured. The default dashboard will be used.",
            onSecondarySettingChanged: OnTriggerChanged);
    }

    protected override void InitDevice(IDevice device)
    {
        if (device is DisplayDevice displayDevice)
        {
            PresetSettings.Initialize(
                _dashboardService.Dashboards.Select(x => new PresetOption(x.Id, x.Name)),
                displayDevice.DashboardId,
                displayDevice.DashboardOverrides);

            PresetSettings.ConfigureSecondarySetting(
                "Auto-Open Trigger",
                GetTriggerOptions(),
                displayDevice.DashboardTrigger.ToString());

            IsDashboardOpen = _dashboardLauncher.IsDeviceDashboardOpen(displayDevice);
        }
        else
        {
            PresetSettings.Initialize([], null, []);
        }

        ToggleDashboardCommand.NotifyCanExecuteChanged();

        // InitDevice can run again when the device list is refreshed.
        _dashboardLauncher.DashboardStateChanged -= OnDashboardStateChanged;
        _dashboardLauncher.DashboardStateChanged += OnDashboardStateChanged;
    }

    private static IEnumerable<PresetOption> GetTriggerOptions()
    {
        return Enum.GetValues<DashboardLaunchTrigger>().Select(x =>
            new PresetOption(x.ToString(), EnumConverters.GetDashboardLaunchTriggerName(x)));
    }

    private void OnTriggerChanged(string? value)
    {
        if (Device is not DisplayDevice displayDevice)
            return;

        displayDevice.DashboardTrigger = Enum.TryParse<DashboardLaunchTrigger>(value, out var trigger)
            ? trigger
            : DashboardLaunchTrigger.None;
        DeviceService.SaveDevice(displayDevice);
    }

    private void OnPresetSettingsChanged()
    {
        if (Device is not DisplayDevice displayDevice)
            return;

        displayDevice.DashboardId = PresetSettings.SelectedPresetId;
        DeviceService.SaveDevice(displayDevice);
        _dashboardLauncher.RefreshDeviceDashboard(displayDevice);
        ToggleDashboardCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanToggleDashboard))]
    private void ToggleDashboard()
    {
        if (Device is not DisplayDevice displayDevice)
            return;

        if (IsDashboardOpen)
            _dashboardLauncher.CloseDeviceDashboard(displayDevice);
        else
            _dashboardLauncher.OpenDeviceDashboard(displayDevice);

        IsDashboardOpen = _dashboardLauncher.IsDeviceDashboardOpen(displayDevice);
    }

    private void OnDashboardStateChanged(object? sender, string deviceId)
    {
        if (Device is not DisplayDevice displayDevice || displayDevice.Id != deviceId)
            return;

        IsDashboardOpen = _dashboardLauncher.IsDeviceDashboardOpen(displayDevice);
        OnPropertyChanged(nameof(DashboardButtonLabel));
    }
}
