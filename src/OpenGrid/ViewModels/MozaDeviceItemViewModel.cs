using CommunityToolkit.Mvvm.ComponentModel;
using Material.Icons;
using OpenGrid.Models;
using OpenGrid.Services.Devices;
using OpenGrid.Services.Devices.Moza;

namespace OpenGrid.ViewModels;

public partial class MozaDeviceItemViewModel : DeviceItemViewModel
{
    private readonly IMozaDeviceService _mozaDeviceService;
    private bool _isInitializing;
    private bool _isInitialized;

    public override MaterialIconKind IconKind => MaterialIconKind.Gauge;

    public IReadOnlyList<int> RpmLedOptions { get; } = Enumerable.Range(1, 16).ToArray();

    public string PortName => (Device as SerialDevice)?.PortName ?? "-";

    public string SerialNumber => (Device as SerialDevice)?.SerialNumber ?? "-";

    public string Category => (Device as SerialDevice)?.Category.ToString() ?? "-";

    [ObservableProperty]
    public partial bool DriveLedsWithTelemetry { get; set; }

    [ObservableProperty]
    public partial int RpmLedCount { get; set; } = 10;

    public MozaDeviceItemViewModel(
        IDeviceService deviceService,
        IMozaDeviceService mozaDeviceService,
        Action<DeviceItemViewModel> onRemove)
        : base(deviceService, onRemove)
    {
        _mozaDeviceService = mozaDeviceService;
    }

    protected override void InitDevice(IDevice device)
    {
        _isInitializing = true;

        if (device is SerialDevice serialDevice)
        {
            DriveLedsWithTelemetry = serialDevice.DriveLedsWithTelemetry;
            RpmLedCount = Math.Clamp(serialDevice.RpmLedCount, 1, RpmLedOptions.Count);
            serialDevice.RpmLedCount = RpmLedCount;
            _mozaDeviceService.ConfigureDevice(serialDevice);
        }
        else
        {
            DriveLedsWithTelemetry = false;
        }

        OnPropertyChanged(nameof(PortName));
        OnPropertyChanged(nameof(SerialNumber));
        OnPropertyChanged(nameof(Category));

        _isInitializing = false;
        _isInitialized = true;
    }

    partial void OnDriveLedsWithTelemetryChanged(bool value)
    {
        SaveAndConfigure();
    }

    partial void OnRpmLedCountChanged(int value)
    {
        SaveAndConfigure();
    }

    protected override void OnDeviceSettingsChanged()
    {
        SaveAndConfigure();
    }

    private void SaveAndConfigure()
    {
        // DeviceItemViewModel.Init sets IsEnabled before InitDevice runs, which
        // raises OnDeviceSettingsChanged. Ignore that callback until the VM has
        // loaded the persisted values, otherwise the defaults overwrite them.
        if (!_isInitialized || _isInitializing || Device is not SerialDevice serialDevice)
        {
            return;
        }

        serialDevice.DriveLedsWithTelemetry = DriveLedsWithTelemetry;
        serialDevice.RpmLedCount = Math.Clamp(RpmLedCount, 1, RpmLedOptions.Count);
        DeviceService.SaveDevice(serialDevice);
        _mozaDeviceService.ConfigureDevice(serialDevice);
    }
}
