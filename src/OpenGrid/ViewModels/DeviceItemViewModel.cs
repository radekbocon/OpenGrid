using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using Material.Icons;
using OpenGrid.Controls;
using OpenGrid.Models;
using OpenGrid.Services.Devices;

namespace OpenGrid.ViewModels;

public abstract partial class DeviceItemViewModel : ViewModelBase
{
    protected readonly IDeviceService DeviceService;
    private readonly Action<DeviceItemViewModel> _onRemove;

    public IDevice? Device { get; private set; }

    public abstract MaterialIconKind IconKind { get; }

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
        if (Device is not { } device)
            return;

        device.IsEnabled = value;
        DeviceService.SaveDevice(device);
    }

    protected DeviceItemViewModel(IDeviceService deviceService, Action<DeviceItemViewModel> onRemove)
    {
        DeviceService = deviceService;
        _onRemove = onRemove;
    }

    public void Init(IDevice device)
    {
        Device = device;
        Name = device.Name;
        Description = device.Description;
        Status = device.Status;
        IsEnabled = device.IsEnabled;
        InitDevice(device);
    }

    protected abstract void InitDevice(IDevice device);

    [RelayCommand]
    private async Task RemoveAsync()
    {
        if (Device is null)
            return;

        var confirmDialog = new ConfirmDialog($"Are you sure you want to remove '{Name}'?");
        await DialogHost.Show(confirmDialog);

        if (confirmDialog.Result)
        {
            DeviceService.RemoveDevice(Device);
            _onRemove.Invoke(this);
        }
    }
}
