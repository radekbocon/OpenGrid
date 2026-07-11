using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using OpenGrid.Controls;
using OpenGrid.Models;
using OpenGrid.Services;

namespace OpenGrid.ViewModels;

public partial class DevicesViewModel : ViewModelBase
{
    private readonly IDeviceService _deviceService;

    public ObservableCollection<DeviceInfo> Devices => _deviceService.Devices;

    public bool HasDevices => Devices.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDeviceSelected))]
    [NotifyCanExecuteChangedFor(nameof(RemoveDeviceCommand))]
    public partial DeviceInfo? SelectedDevice { get; set; }

    public bool IsDeviceSelected => SelectedDevice is not null;

    public DevicesViewModel(IDeviceService deviceService)
    {
        _deviceService = deviceService;
        IsMenuItem = true;
    }

    [RelayCommand]
    private async Task AddDeviceAsync()
    {
        var discovered = _deviceService.ScanForDevices();
        var existingIds = Devices.Select(d => d.Id).ToHashSet();
        var available = discovered.Where(d => !existingIds.Contains(d.Id)).ToList();

        var dialog = new AddDeviceDialog(available);
        await DialogHost.Show(dialog);

        if (dialog.SelectedDevice is { } device)
        {
            _deviceService.AddDevice(device);
            SelectedDevice = device;
            OnPropertyChanged(nameof(HasDevices));
        }
    }

    [RelayCommand(CanExecute = nameof(IsDeviceSelected))]
    private async Task RemoveDeviceAsync()
    {
        if (SelectedDevice is not { } device)
            return;

        var confirmDialog = new ConfirmDialog($"Remove device \"{device.Name}\"?");
        await DialogHost.Show(confirmDialog);

        if (confirmDialog.Result)
        {
            var wasSelected = device == SelectedDevice;
            _deviceService.RemoveDevice(device.Id);
            if (wasSelected)
                SelectedDevice = Devices.FirstOrDefault();
            OnPropertyChanged(nameof(HasDevices));
        }
    }

    partial void OnSelectedDeviceChanged(DeviceInfo? value)
    {
        OnPropertyChanged(nameof(IsDeviceSelected));
    }
}
