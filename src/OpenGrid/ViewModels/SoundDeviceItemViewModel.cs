using Material.Icons;
using OpenGrid.Models;
using OpenGrid.Services;
using OpenGrid.Services.Devices;
using OpenGrid.Services.Sound;

namespace OpenGrid.ViewModels;

public partial class SoundDeviceItemViewModel : DeviceItemViewModel
{
    private readonly IBassShakerService _bassShakerService;
    private readonly IBassShakerPresetService _presetService;

    public override MaterialIconKind IconKind => MaterialIconKind.VolumeHigh;

    public DevicePresetSettingsViewModel PresetSettings { get; }

    public SoundDeviceItemViewModel(
        IDeviceService deviceService,
        IBassShakerService bassShakerService,
        IBassShakerPresetService presetService,
        ICarConfigService carConfigService,
        Action<DeviceItemViewModel> onRemove)
        : base(deviceService, onRemove)
    {
        _bassShakerService = bassShakerService;
        _presetService = presetService;

        PresetSettings = new DevicePresetSettingsViewModel(
            carConfigService,
            static id => new BassShakerPresetOverride { PresetId = id },
            OnPresetSettingsChanged);
    }

    protected override void InitDevice(IDevice device)
    {
        if (device is not SoundDevice soundDevice)
        {
            PresetSettings.Initialize([], null, []);
            return;
        }

        PresetSettings.Initialize(
            _presetService.GetAll().Select(x => new PresetOption(x)),
            soundDevice.DefaultBassShakerPresetId,
            soundDevice.BassShakerOverrides);

        ConfigureBassShaker();
    }

    protected override void OnDeviceSettingsChanged()
    {
        ConfigureBassShaker();
    }

    private void OnPresetSettingsChanged()
    {
        if (Device is not SoundDevice soundDevice)
        {
            return;
        }

        soundDevice.DefaultBassShakerPresetId = PresetSettings.SelectedPresetId;
        DeviceService.SaveDevice(soundDevice);
        ConfigureBassShaker();
    }

    private void ConfigureBassShaker()
    {
        if (Device is SoundDevice soundDevice)
        {
            _bassShakerService.ConfigureDevice(soundDevice);
        }
    }
}
