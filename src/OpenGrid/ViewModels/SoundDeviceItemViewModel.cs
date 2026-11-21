using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using Material.Icons;
using OpenGrid.Controls;
using OpenGrid.Models;
using OpenGrid.Models.Telemetry;
using OpenGrid.Services;
using OpenGrid.Services.Devices;
using OpenGrid.Services.Sound;

namespace OpenGrid.ViewModels;

public partial class SoundDeviceItemViewModel : DeviceItemViewModel
{
    private readonly IBassShakerService _bassShakerService;
    private readonly IBassShakerPresetService _presetService;
    private readonly ICarConfigService _carConfigService;
    private bool _isRefreshingPresets;

    public override MaterialIconKind IconKind => MaterialIconKind.VolumeHigh;

    public ObservableCollection<BassShakerPresetOption> DefaultPresetOptions { get; } = [];
    public ObservableCollection<BassShakerPresetOption> PresetOptions { get; } = [];
    public ObservableCollection<BassShakerOverrideItemViewModel> BassShakerOverrides { get; } = [];

    [ObservableProperty]
    public partial BassShakerPresetOption? SelectedDefaultPreset { get; set; }

    public bool HasPresets => PresetOptions.Count > 0;
    public bool HasBassShakerOverrides => BassShakerOverrides.Count > 0;

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
        _carConfigService = carConfigService;
    }

    protected override void InitDevice(IDevice device)
    {
        if (device is not SoundDevice soundDevice)
        {
            RefreshPresetOptions(null);
            BassShakerOverrides.Clear();
            return;
        }

        RefreshPresetOptions(soundDevice);
        RefreshOverrides(soundDevice);
        ConfigureBassShaker();
    }

    partial void OnSelectedDefaultPresetChanged(BassShakerPresetOption? value)
    {
        if (_isRefreshingPresets || Device is not SoundDevice soundDevice)
        {
            return;
        }

        soundDevice.DefaultBassShakerPresetId = value?.Preset?.Id;
        DeviceService.SaveDevice(soundDevice);
        ConfigureBassShaker();
    }

    protected override void OnDeviceSettingsChanged()
    {
        ConfigureBassShaker();
    }

    [RelayCommand]
    private async Task AddBassShakerOverrideAsync()
    {
        if (Device is not SoundDevice soundDevice || PresetOptions.Count == 0)
        {
            return;
        }

        var dialog = new AddBassShakerOverrideDialog(
            SteamGame.GetAllSupported(),
            _carConfigService.GetAll(),
            PresetOptions.Select(x => x.Preset).OfType<BassShakerPreset>());
        await DialogHost.Show(dialog);

        if (dialog.Result is not { } result)
        {
            return;
        }

        var existing = soundDevice.BassShakerOverrides.FirstOrDefault(x =>
            x.GameAppId == result.GameAppId &&
            x.Car == result.Car);

        if (existing is null)
        {
            soundDevice.BassShakerOverrides.Add(result);
        }
        else
        {
            existing.PresetId = result.PresetId;
        }

        DeviceService.SaveDevice(soundDevice);
        RefreshOverrides(soundDevice);
        ConfigureBassShaker();
    }

    private void RemoveOverride(BassShakerOverrideItemViewModel item)
    {
        if (Device is not SoundDevice soundDevice)
        {
            return;
        }

        soundDevice.BassShakerOverrides.Remove(item.Model);
        BassShakerOverrides.Remove(item);
        OnPropertyChanged(nameof(HasBassShakerOverrides));
        SaveAndConfigure();
    }

    private void RefreshPresetOptions(SoundDevice? soundDevice)
    {
        _isRefreshingPresets = true;

        DefaultPresetOptions.Clear();
        PresetOptions.Clear();
        DefaultPresetOptions.Add(BassShakerPresetOption.None);

        foreach (var preset in _presetService.GetAll().OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            var option = new BassShakerPresetOption(preset);
            DefaultPresetOptions.Add(option);
            PresetOptions.Add(option);
        }

        var defaultPresetId = soundDevice?.DefaultBassShakerPresetId;
        var selectedOption = string.IsNullOrWhiteSpace(defaultPresetId)
            ? null
            : DefaultPresetOptions.FirstOrDefault(x => x.Id == defaultPresetId);

        if (selectedOption is null &&
            soundDevice is not null &&
            !string.IsNullOrWhiteSpace(defaultPresetId))
        {
            soundDevice.DefaultBassShakerPresetId = null;
            DeviceService.SaveDevice(soundDevice);
        }

        SelectedDefaultPreset = selectedOption ?? BassShakerPresetOption.None;

        _isRefreshingPresets = false;
        OnPropertyChanged(nameof(HasPresets));
    }

    private void RefreshOverrides(SoundDevice soundDevice)
    {
        BassShakerOverrides.Clear();

        foreach (var gameOverride in soundDevice.BassShakerOverrides)
        {
            var game = gameOverride.GameAppId is { } appId
                ? SteamGame.GetByAppId(appId)
                : null;
            var gameName = gameOverride.GameAppId is null
                ? "Any game"
                : game?.Name ?? "Unknown game";

            var carName = gameOverride.Car is null
                ? "Any car"
                : gameOverride.Car.Name;

            BassShakerOverrides.Add(new BassShakerOverrideItemViewModel(
                gameOverride,
                gameName,
                carName,
                PresetOptions,
                SaveOverrides,
                RemoveOverride));
        }

        OnPropertyChanged(nameof(HasBassShakerOverrides));
    }

    private void SaveOverrides()
    {
        SaveAndConfigure();
    }

    private void SaveAndConfigure()
    {
        if (Device is not SoundDevice soundDevice)
        {
            return;
        }

        DeviceService.SaveDevice(soundDevice);
        ConfigureBassShaker();
    }

    private void ConfigureBassShaker()
    {
        if (Device is not SoundDevice soundDevice)
        {
            return;
        }

        _bassShakerService.ConfigureDevice(soundDevice);
    }
}

public sealed class BassShakerOverrideItemViewModel : ViewModelBase
{
    private readonly Action _save;
    private readonly Action<BassShakerOverrideItemViewModel> _remove;
    private BassShakerPresetOption? _selectedPreset;

    public BassShakerPresetOverride Model { get; }
    public string GameName { get; }
    public string CarName { get; }
    public IReadOnlyList<BassShakerPresetOption> PresetOptions { get; }
    public IRelayCommand RemoveCommand { get; }

    public BassShakerPresetOption? SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (!SetProperty(ref _selectedPreset, value))
            {
                return;
            }

            Model.PresetId = value?.Id ?? string.Empty;
            _save();
        }
    }

    public BassShakerOverrideItemViewModel(
        BassShakerPresetOverride model,
        string gameName,
        string carName,
        IReadOnlyList<BassShakerPresetOption> presetOptions,
        Action save,
        Action<BassShakerOverrideItemViewModel> remove)
    {
        Model = model;
        GameName = gameName;
        CarName = carName;
        PresetOptions = presetOptions;
        _save = save;
        _remove = remove;
        _selectedPreset = presetOptions.FirstOrDefault(x => x.Id == model.PresetId);
        RemoveCommand = new RelayCommand(() => _remove(this));
    }
}
