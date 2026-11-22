using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using OpenGrid.Controls;
using OpenGrid.Models;
using OpenGrid.Services;
using OpenGrid.Services.Dashboard;
using OpenGrid.Services.Devices;
using OpenGrid.Services.Devices.Moza;
using OpenGrid.Services.Sound;

namespace OpenGrid.ViewModels;

public partial class DevicesViewModel : ViewModelBase
{
    private readonly IDeviceService _deviceService;
    private readonly IMozaDeviceService _mozaDeviceService;
    private readonly IDashboardService _dashboardService;
    private readonly DashboardLauncher _dashboardLauncher;
    private readonly IBassShakerService _bassShakerService;
    private readonly IBassShakerPresetService _bassShakerPresetService;
    private readonly ICarConfigService _carConfigService;
    private bool _isStatusSubscribed;

    public ObservableCollection<DeviceItemViewModel> Devices { get; }

    public bool HasDevices => Devices.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDeviceSelected))]
    [NotifyPropertyChangedFor(nameof(HasDevices))]
    public partial DeviceItemViewModel? SelectedDevice { get; set; }

    public bool IsDeviceSelected => SelectedDevice is not null;

    public DevicesViewModel(IDeviceService deviceService, IMozaDeviceService mozaDeviceService, IDashboardService dashboardService, DashboardLauncher dashboardLauncher, IBassShakerService bassShakerService, IBassShakerPresetService bassShakerPresetService, ICarConfigService carConfigService)
    {
        _deviceService = deviceService;
        _mozaDeviceService = mozaDeviceService;
        _dashboardService = dashboardService;
        _dashboardLauncher = dashboardLauncher;
        _bassShakerService = bassShakerService;
        _bassShakerPresetService = bassShakerPresetService;
        _carConfigService = carConfigService;
        IsMenuItem = true;
        Devices = [];
    }

    protected override Task OnLoadedAsync()
    {
        if (!_isStatusSubscribed)
        {
            _mozaDeviceService.StatusChanged += OnMozaStatusChanged;
            _isStatusSubscribed = true;
        }

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

    protected override Task OnUnloadedAsync()
    {
        if (_isStatusSubscribed)
        {
            _mozaDeviceService.StatusChanged -= OnMozaStatusChanged;
            _isStatusSubscribed = false;
        }

        return base.OnUnloadedAsync();
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
        DeviceItemViewModel deviceItem = device.DeviceType switch
        {
            DeviceType.Display => new DisplayDeviceItemViewModel(_deviceService, _dashboardService, _dashboardLauncher, OnRemove),
            DeviceType.Serial => new MozaDeviceItemViewModel(_deviceService, _mozaDeviceService, OnRemove),
            DeviceType.Sound => new SoundDeviceItemViewModel(_deviceService, _bassShakerService, _bassShakerPresetService, _carConfigService, OnRemove),
            _ => throw new ArgumentOutOfRangeException(nameof(device.DeviceType), device.DeviceType, "Unsupported device type"),
        };
        deviceItem.Init(device);
        return deviceItem;
    }

    private void OnRemove(DeviceItemViewModel item)
    {
        if (item.Device is SoundDevice soundDevice)
        {
            _bassShakerService.RemoveDevice(soundDevice.Id);
        }
        else if (item.Device is SerialDevice serialDevice)
        {
            _mozaDeviceService.RemoveDevice(serialDevice.Id);
        }

        Devices.Remove(item);
        SelectedDevice = Devices.FirstOrDefault();
        OnPropertyChanged(nameof(IsDeviceSelected));
        OnPropertyChanged(nameof(HasDevices));
    }

    private void OnMozaStatusChanged(object? sender, string deviceId)
    {
        var item = Devices.FirstOrDefault(x => x.Device?.Id == deviceId);
        if (item?.Device is null)
        {
            return;
        }

        item.Status = item.Device.Status;
    }
}
