using CommunityToolkit.Mvvm.ComponentModel;
using Material.Icons;
using OpenGrid.Models;
using OpenGrid.Services;
using OpenGrid.Services.Devices;
using OpenGrid.Services.Devices.Moza;

namespace OpenGrid.ViewModels;

public partial class MozaDeviceItemViewModel : DeviceItemViewModel
{
    private const int MaxLeds = 16;

    private readonly IMozaDeviceService _mozaDeviceService;
    private readonly ILedPresetService _presetService;
    private readonly IRpmLedEffectService _rpmLedEffectService;
    private bool _isInitializing;
    private bool _isInitialized;

    public override MaterialIconKind IconKind => MaterialIconKind.Gauge;

    public IReadOnlyList<int> LedCountOptions { get; } =
        Enumerable.Range(1, MaxLeds).ToArray();

    public string PortName => (Device as MozaSerialDevice)?.PortName ?? "-";

    public string SerialNumber => (Device as MozaSerialDevice)?.SerialNumber ?? "-";

    public string Category => (Device as MozaSerialDevice)?.Category.ToString() ?? "-";

    public DevicePresetSettingsViewModel PresetSettings { get; }

    [ObservableProperty]
    private bool _driveLedsWithTelemetry;

    [ObservableProperty]
    private int _ledCount = 10;

    public MozaDeviceItemViewModel(
        IDeviceService deviceService,
        IMozaDeviceService mozaDeviceService,
        ILedPresetService presetService,
        IRpmLedEffectService rpmLedEffectService,
        ICarConfigService carConfigService,
        Action<DeviceItemViewModel> onRemove)
        : base(deviceService, onRemove)
    {
        _mozaDeviceService = mozaDeviceService;
        _presetService = presetService;
        _rpmLedEffectService = rpmLedEffectService;

        PresetSettings = new DevicePresetSettingsViewModel(
            carConfigService,
            static id => new LedPresetOverride { PresetId = id },
            OnPresetSettingsChanged);
    }

    protected override void InitDevice(IDevice device)
    {
        _isInitializing = true;

        if (device is MozaSerialDevice serialDevice)
        {
            DriveLedsWithTelemetry = serialDevice.DriveLedsWithTelemetry;
            LedCount = Math.Clamp(serialDevice.RpmLedCount, 1, MaxLeds);
            serialDevice.RpmLedCount = LedCount;

            PresetSettings.Initialize(
                _presetService.GetAll().Select(x => new PresetOption(x)),
                serialDevice.DefaultLedPresetId,
                serialDevice.LedOverrides);

            _mozaDeviceService.ConfigureDeviceAsync(serialDevice).FireAndForgetSafe();
        }
        else
        {
            DriveLedsWithTelemetry = false;
            PresetSettings.Initialize([], null, []);
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

    partial void OnLedCountChanged(int value)
    {
        if (_isInitializing || !_isInitialized)
        {
            return;
        }

        SaveAndConfigure();
    }

    protected override void OnDeviceSettingsChanged()
    {
        SaveAndConfigure();
    }

    private void OnPresetSettingsChanged()
    {
        if (Device is not MozaSerialDevice serialDevice)
        {
            return;
        }

        serialDevice.DefaultLedPresetId = PresetSettings.SelectedPresetId;
        _rpmLedEffectService.Reset(serialDevice.Id);
        SaveAndConfigure();
    }

    private void SaveAndConfigure()
    {
        // DeviceItemViewModel.Init sets IsEnabled before InitDevice runs, which
        // raises OnDeviceSettingsChanged. Ignore that callback until the VM has
        // loaded the persisted values, otherwise the defaults overwrite them.
        if (!_isInitialized || _isInitializing || Device is not MozaSerialDevice serialDevice)
        {
            return;
        }

        serialDevice.DriveLedsWithTelemetry = DriveLedsWithTelemetry;
        serialDevice.RpmLedCount = Math.Clamp(LedCount, 1, MaxLeds);
        DeviceService.SaveDevice(serialDevice);
        _mozaDeviceService.ConfigureDeviceAsync(serialDevice).FireAndForgetSafe();
    }
}
