using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using OpenGrid.Controls;
using OpenGrid.Models;
using OpenGrid.Services.Dashboard;
using OpenGrid.Services.Devices;

namespace OpenGrid.ViewModels;

public partial class DevicesViewModel : ViewModelBase
{
    private readonly IDeviceService _deviceService;
    private readonly IDashboardService _dashboardService;
    private readonly DashboardLauncher _dashboardLauncher;

    public ObservableCollection<DeviceItemViewModel> Devices { get; }

    public bool HasDevices => Devices.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDeviceSelected))]
    [NotifyPropertyChangedFor(nameof(HasDevices))]
    public partial DeviceItemViewModel? SelectedDevice { get; set; }

    public bool IsDeviceSelected => SelectedDevice is not null;

    public DevicesViewModel(IDeviceService deviceService, IDashboardService dashboardService, DashboardLauncher dashboardLauncher)
    {
        _deviceService = deviceService;
        _dashboardService = dashboardService;
        _dashboardLauncher = dashboardLauncher;
        IsMenuItem = true;
        Devices = [];
    }

    protected override Task OnLoadedAsync()
    {
        Devices.Clear();
        var devices = _deviceService.GetPersistedDevices();
        foreach (var device in devices)
        {
            Devices.Add(CreateDeviceItem(device));
        }
        OnPropertyChanged(nameof(HasDevices));
        SelectedDevice = Devices.FirstOrDefault();
        return base.OnLoadedAsync();
    }

    [RelayCommand]
    private async Task AddDeviceAsync()
    {
        var available = _deviceService.ScanForDevices();

        var dialog = new AddDeviceDialog(available.ToList());
        await DialogHost.Show(dialog);

        if (dialog.SelectedDevice is { } device)
        {
            _deviceService.AddDevice(device);
            var deviceItem = CreateDeviceItem(device);
            Devices.Add(deviceItem);
            SelectedDevice = deviceItem;
            OnPropertyChanged(nameof(IsDeviceSelected));
            OnPropertyChanged(nameof(HasDevices));
        }
    }

    [RelayCommand]
    private void DeviceSelected(DeviceItemViewModel? device)
    {
        SelectedDevice = device;
    }

    private DeviceItemViewModel CreateDeviceItem(IDevice device)
    {
        var deviceItem = new DeviceItemViewModel(_deviceService, _dashboardService, _dashboardLauncher, OnRemove);
        deviceItem.Init(device);
        return deviceItem;
    }

    private void OnRemove(DeviceItemViewModel item)
    {
        Devices.Remove(item);
        SelectedDevice = Devices.FirstOrDefault();
        OnPropertyChanged(nameof(IsDeviceSelected));
        OnPropertyChanged(nameof(HasDevices));
    }
}
