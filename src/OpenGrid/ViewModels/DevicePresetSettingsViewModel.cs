using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using OpenGrid.Controls;
using OpenGrid.Models;
using OpenGrid.Models.Telemetry;
using OpenGrid.Services;

namespace OpenGrid.ViewModels;

/// <summary>
/// Default preset and game/car override editor shared by every device that
/// supports presets.
/// </summary>
public sealed partial class DevicePresetSettingsViewModel : ViewModelBase
{
    private readonly ICarConfigService _carConfigService;
    private readonly Func<string, PresetOverride> _createOverride;
    private readonly Action _onChanged;
    private readonly Action<string?>? _onSecondarySettingChanged;
    private bool _isRefreshing;

    public ObservableCollection<PresetOption> DefaultPresetOptions { get; } = [];
    public ObservableCollection<PresetOption> PresetOptions { get; } = [];
    public ObservableCollection<PresetOverrideItemViewModel> Overrides { get; } = [];
    public ObservableCollection<PresetOption> SecondaryOptions { get; } = [];

    [ObservableProperty]
    private PresetOption? _selectedDefaultPreset;

    [ObservableProperty]
    private PresetOption? _selectedSecondaryOption;

    /// <summary>Null represents the "None" option.</summary>
    public string? SelectedPresetId => SelectedDefaultPreset is { Id: { Length: > 0 } id } ? id : null;

    public bool HasPresets => PresetOptions.Count > 0;
    public bool HasOverrides => Overrides.Count > 0;
    public bool HasSecondarySetting => SecondaryOptions.Count > 0;

    /// <summary>Name of the item being selected, e.g. "Preset" or "Dashboard".</summary>
    public string ItemLabel { get; }
    public string DefaultItemLabel => $"Default {ItemLabel}";
    public string? ItemsLocationHint { get; }
    public string EmptyOverridesHint { get; }
    public string? SecondaryLabel { get; private set; }

    public DevicePresetSettingsViewModel(
        ICarConfigService carConfigService,
        Func<string, PresetOverride> createOverride,
        Action onChanged,
        string itemLabel = "Preset",
        string? itemsLocationHint = "Presets are created and edited on the Presets page.",
        string emptyOverridesHint = "No game or car overrides configured. The default preset will be used.",
        Action<string?>? onSecondarySettingChanged = null)
    {
        _carConfigService = carConfigService;
        _createOverride = createOverride;
        _onChanged = onChanged;
        _onSecondarySettingChanged = onSecondarySettingChanged;
        ItemLabel = itemLabel;
        ItemsLocationHint = itemsLocationHint;
        EmptyOverridesHint = emptyOverridesHint;
    }

    public void Initialize(
        IEnumerable<PresetOption> presets,
        string? defaultPresetId,
        IEnumerable<PresetOverride> overrides)
    {
        _isRefreshing = true;

        DefaultPresetOptions.Clear();
        PresetOptions.Clear();
        SecondaryOptions.Clear();
        SecondaryLabel = null;
        SelectedSecondaryOption = null;
        DefaultPresetOptions.Add(PresetOption.None);

        foreach (var preset in presets.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            var option = new PresetOption(preset.Id, preset.Name);
            DefaultPresetOptions.Add(option);
            PresetOptions.Add(option);
        }

        var selectedOption = string.IsNullOrWhiteSpace(defaultPresetId)
            ? null
            : DefaultPresetOptions.FirstOrDefault(x => x.Id == defaultPresetId);

        SelectedDefaultPreset = selectedOption ?? PresetOption.None;

        Overrides.Clear();
        foreach (var item in overrides)
        {
            AddOverrideItem(item);
        }

        _isRefreshing = false;
        OnPropertyChanged(nameof(HasPresets));
        OnPropertyChanged(nameof(HasOverrides));
        OnPropertyChanged(nameof(HasSecondarySetting));
    }

    /// <summary>
    /// Optional dropdown shown beside the default selector (e.g. the dashboard
    /// auto-open trigger). Devices that do not need it simply leave it empty.
    /// </summary>
    public void ConfigureSecondarySetting(
        string label,
        IEnumerable<PresetOption> options,
        string? selectedId)
    {
        _isRefreshing = true;

        SecondaryLabel = label;
        SecondaryOptions.Clear();
        foreach (var option in options)
        {
            SecondaryOptions.Add(option);
        }

        SelectedSecondaryOption = SecondaryOptions.FirstOrDefault(x => x.Id == selectedId)
                                  ?? SecondaryOptions.FirstOrDefault();

        _isRefreshing = false;
        OnPropertyChanged(nameof(SecondaryLabel));
        OnPropertyChanged(nameof(HasSecondarySetting));
    }

    partial void OnSelectedDefaultPresetChanged(PresetOption? value)
    {
        OnPropertyChanged(nameof(SelectedPresetId));
        if (!_isRefreshing)
        {
            _onChanged();
        }
    }

    partial void OnSelectedSecondaryOptionChanged(PresetOption? value)
    {
        if (!_isRefreshing && value is not null)
        {
            _onSecondarySettingChanged?.Invoke(value.Id);
        }
    }

    [RelayCommand]
    private async Task AddOverrideAsync()
    {
        if (PresetOptions.Count == 0)
        {
            return;
        }

        var dialog = new AddPresetOverrideDialog(
            SteamGame.GetAllSupported(),
            _carConfigService.GetAll(),
            PresetOptions,
            _createOverride,
            ItemLabel);
        await DialogHost.Show(dialog);

        if (dialog.Result is not { } result)
        {
            return;
        }

        Overrides.Add(new PresetOverrideItemViewModel(
            result,
            GetGameName(result.GameAppId),
            GetCarName(result.Car),
            PresetOptions,
            _onChanged,
            RemoveOverride,
            ItemLabel));

        OnPropertyChanged(nameof(HasOverrides));
        _onChanged();
    }

    private void RemoveOverride(PresetOverrideItemViewModel item)
    {
        if (!Overrides.Remove(item))
        {
            return;
        }

        OnPropertyChanged(nameof(HasOverrides));
        _onChanged();
    }

    private void AddOverrideItem(PresetOverride item)
    {
        Overrides.Add(new PresetOverrideItemViewModel(
            item,
            GetGameName(item.GameAppId),
            GetCarName(item.Car),
            PresetOptions,
            _onChanged,
            RemoveOverride,
            ItemLabel));
    }

    private static string GetGameName(int? appId)
    {
        if (appId is null)
        {
            return "Any game";
        }

        return SteamGame.GetByAppId(appId.Value)?.Name ?? "Unknown game";
    }

    private static string GetCarName(CarProfile? car) => car?.Name ?? "Any car";
}

public sealed class PresetOverrideItemViewModel : ViewModelBase
{
    private readonly Action _save;
    private readonly Action<PresetOverrideItemViewModel> _remove;
    private PresetOption? _selectedPreset;

    public PresetOverride Model { get; }
    public string GameName { get; }
    public string CarName { get; }
    public string ItemLabel { get; }
    public IReadOnlyList<PresetOption> PresetOptions { get; }
    public IRelayCommand RemoveCommand { get; }

    public PresetOption? SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (!SetProperty(ref _selectedPreset, value) || value is null)
            {
                return;
            }

            Model.PresetId = value.Id;
            _save();
        }
    }

    public PresetOverrideItemViewModel(
        PresetOverride model,
        string gameName,
        string carName,
        IReadOnlyList<PresetOption> presetOptions,
        Action save,
        Action<PresetOverrideItemViewModel> remove,
        string itemLabel = "Preset")
    {
        Model = model;
        GameName = gameName;
        CarName = carName;
        ItemLabel = itemLabel;
        PresetOptions = presetOptions;
        _save = save;
        _remove = remove;
        _selectedPreset = presetOptions.FirstOrDefault(x => x.Id == model.PresetId);
        RemoveCommand = new RelayCommand(() => _remove(this));
    }
}
