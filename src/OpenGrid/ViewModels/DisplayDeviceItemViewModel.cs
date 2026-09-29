using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using OpenGrid.Models;
using OpenGrid.Services.Dashboard;
using OpenGrid.Services.Devices;

namespace OpenGrid.ViewModels;

public partial class DisplayDeviceItemViewModel : DeviceItemViewModel
{
    private static readonly DashboardInfo NoneDashboard = new() { Id = "", Name = "None", Description = "", DirectoryPath = "" };

    private readonly IDashboardService _dashboardService;
    private readonly DashboardLauncher _dashboardLauncher;

    public override MaterialIconKind IconKind => MaterialIconKind.Monitor;

    public ObservableCollection<DashboardInfo> AvailableDashboards { get; } = [];

    public ObservableCollection<DashboardInfo> DashboardOptions { get; } = [];

    public static DashboardLaunchTrigger[] AvailableTriggers { get; } =
        Enum.GetValues<DashboardLaunchTrigger>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedDashboard))]
    public partial DashboardInfo? SelectedDashboard { get; set; }

    [ObservableProperty]
    public partial DashboardInfo? SelectedDashboardOption { get; set; }

    [ObservableProperty]
    public partial DashboardLaunchTrigger SelectedTrigger { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDashboardOpen))]
    [NotifyPropertyChangedFor(nameof(DashboardButtonLabel))]
    [NotifyCanExecuteChangedFor(nameof(ToggleDashboardCommand))]
    public partial bool IsDashboardOpen { get; set; }

    public bool HasSelectedDashboard => SelectedDashboard is not null;

    public string DashboardButtonLabel => IsDashboardOpen ? "Close Dashboard" : "Launch Dashboard";

    partial void OnSelectedDashboardChanged(DashboardInfo? value)
    {
        if (Device is not DisplayDevice displayDevice)
            return;

        displayDevice.DashboardId = value?.Id;
        DeviceService.SaveDevice(displayDevice);
        OnPropertyChanged(nameof(HasSelectedDashboard));
    }

    partial void OnSelectedDashboardOptionChanged(DashboardInfo? value)
    {
        SelectedDashboard = value?.Id == NoneDashboard.Id ? null : value;
    }

    partial void OnSelectedTriggerChanged(DashboardLaunchTrigger value)
    {
        if (Device is not DisplayDevice displayDevice)
            return;

        displayDevice.DashboardTrigger = value;
        DeviceService.SaveDevice(displayDevice);
    }

    public DisplayDeviceItemViewModel(IDeviceService deviceService, IDashboardService dashboardService, DashboardLauncher dashboardLauncher, Action<DeviceItemViewModel> onRemove)
        : base(deviceService, onRemove)
    {
        _dashboardService = dashboardService;
        _dashboardLauncher = dashboardLauncher;
    }

    protected override void InitDevice(IDevice device)
    {
        AvailableDashboards.Clear();
        DashboardOptions.Clear();
        DashboardOptions.Add(NoneDashboard);
        foreach (var dashboard in _dashboardService.Dashboards)
        {
            AvailableDashboards.Add(dashboard);
            DashboardOptions.Add(dashboard);
        }

        if (device is DisplayDevice displayDevice)
        {
            SelectedDashboard = !string.IsNullOrEmpty(displayDevice.DashboardId)
                ? _dashboardService.GetById(displayDevice.DashboardId)
                : null;
            SelectedDashboardOption = SelectedDashboard ?? NoneDashboard;
            SelectedTrigger = displayDevice.DashboardTrigger;
            IsDashboardOpen = _dashboardLauncher.IsDeviceDashboardOpen(displayDevice.Id);
        }
        else
        {
            SelectedDashboardOption = NoneDashboard;
        }

        _dashboardLauncher.DashboardStateChanged += OnDashboardStateChanged;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedDashboard))]
    private void ToggleDashboard()
    {
        if (Device is not DisplayDevice displayDevice)
            return;

        if (IsDashboardOpen)
            _dashboardLauncher.CloseDeviceDashboard(displayDevice.Id);
        else
            _dashboardLauncher.OpenDeviceDashboard(displayDevice);

        IsDashboardOpen = _dashboardLauncher.IsDeviceDashboardOpen(displayDevice.Id);
    }

    private void OnDashboardStateChanged(object? sender, string deviceId)
    {
        if (Device is not DisplayDevice displayDevice)
            return;

        if (displayDevice.Id != deviceId)
            return;

        IsDashboardOpen = _dashboardLauncher.IsDeviceDashboardOpen(displayDevice.Id);
        OnPropertyChanged(nameof(DashboardButtonLabel));
    }
}
