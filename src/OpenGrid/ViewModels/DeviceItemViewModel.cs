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
    private readonly IDeviceService _deviceService;
    private readonly IDashboardService _dashboardService;
    private readonly Action<DeviceItemViewModel> _onRemove;

    public IDevice? Device { get; private set; }

    public ObservableCollection<DashboardInfo> AvailableDashboards { get; } = [];

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
    public partial DashboardLaunchTrigger SelectedTrigger { get; set; }

    public bool HasSelectedDashboard => SelectedDashboard is not null;

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

    partial void OnSelectedTriggerChanged(DashboardLaunchTrigger value)
    {
        if (Device is not DisplayDevice displayDevice)
            return;

        displayDevice.DashboardTrigger = value;
        _deviceService.SaveDevice(displayDevice);
    }

    public DeviceItemViewModel(IDeviceService deviceService, IDashboardService dashboardService, Action<DeviceItemViewModel> onRemove)
    {
        _deviceService = deviceService;
        _dashboardService = dashboardService;
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
        foreach (var dashboard in _dashboardService.Dashboards)
        {
            AvailableDashboards.Add(dashboard);
        }

        if (device is DisplayDevice displayDevice)
        {
            SelectedDashboard = !string.IsNullOrEmpty(displayDevice.DashboardId)
                ? _dashboardService.GetById(displayDevice.DashboardId)
                : null;
            SelectedTrigger = displayDevice.DashboardTrigger;
        }
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
}
