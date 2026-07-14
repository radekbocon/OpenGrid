using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using OpenGrid.Controls;
using OpenGrid.Models;
using OpenGrid.Services;
using OpenGrid.Services.Devices;

namespace OpenGrid.ViewModels;

public partial class DeviceItemViewModel : ViewModelBase
{
    private readonly IDeviceService _deviceService;
    private readonly Action<DeviceItemViewModel> _onRemove;

    public IDevice? Device { get; private set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? Description { get; set; }

    [ObservableProperty]
    public partial DeviceConnectionStatus Status { get; set; }

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    partial void OnIsEnabledChanged(bool value)
    {
        if (Device is { } device)
        {
            device.IsEnabled = value;
            _deviceService.SaveDevice(device);
        }
    }

    public DeviceItemViewModel(IDeviceService deviceService, Action<DeviceItemViewModel> onRemove)
    {
        _deviceService = deviceService;
        _onRemove = onRemove;
    }

    public void Init(IDevice device)
    {
        Device = device;
        Name = device.Name;
        Description = device.Description;
        Status = device.Status;
        IsEnabled = device.IsEnabled;
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
