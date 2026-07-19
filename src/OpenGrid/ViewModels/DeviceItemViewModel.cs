using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using OpenGrid.Controls;
using OpenGrid.Models;
using OpenGrid.Services.Dashboard;
using OpenGrid.Services.Devices;

namespace OpenGrid.ViewModels;

public partial class DeviceItemViewModel : ViewModelBase
{
    private static readonly DashboardInfo NoneDashboard = new() { Id = "", Name = "None", Description = "", DirectoryPath = "" };

    private readonly IDeviceService _deviceService;
    private readonly IDashboardService _dashboardService;
    private readonly DashboardLauncher _dashboardLauncher;
    private readonly Action<DeviceItemViewModel> _onRemove;

    public IDevice? Device { get; private set; }

    public ObservableCollection<DashboardInfo> AvailableDashboards { get; } = [];

    public ObservableCollection<DashboardInfo> DashboardOptions { get; } = [];

    public static DashboardLaunchTrigger[] AvailableTriggers { get; } =
        Enum.GetValues<DashboardLaunchTrigger>();

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? Description { get; set; }

    [ObservableProperty]
    public partial DeviceConnectionStatus Status { get; set; }

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

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

    partial void OnIsEnabledChanged(bool value)
    {
        if (Device is not { } device)
            return;

        device.IsEnabled = value;
        _deviceService.SaveDevice(device);
    }

    partial void OnSelectedDashboardChanged(DashboardInfo? value)
    {
        if (Device is not DisplayDevice displayDevice)
            return;

        displayDevice.DashboardId = value?.Id;
        _deviceService.SaveDevice(displayDevice);
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
        _deviceService.SaveDevice(displayDevice);
    }

    public DeviceItemViewModel(IDeviceService deviceService, IDashboardService dashboardService, DashboardLauncher dashboardLauncher, Action<DeviceItemViewModel> onRemove)
    {
        _deviceService = deviceService;
        _dashboardService = dashboardService;
        _dashboardLauncher = dashboardLauncher;
        _onRemove = onRemove;
    }

    public void Init(IDevice device)
    {
        Device = device;
        Name = device.Name;
        Description = device.Description;
        Status = device.Status;
        IsEnabled = device.IsEnabled;

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

    [RelayCommand]
    private async Task RemoveAsync()
    {
        if (Device is null)
            return;

        var confirmDialog = new ConfirmDialog($"Are you sure you want to remove '{Name}'?");
        await DialogHost.Show(confirmDialog);

        if (confirmDialog.Result)
        {
            _deviceService.RemoveDevice(Device);
            _onRemove.Invoke(this);
        }
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
