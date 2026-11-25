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
    private bool _isRefreshing;

    public ObservableCollection<PresetOption> DefaultPresetOptions { get; } = [];
    public ObservableCollection<PresetOption> PresetOptions { get; } = [];
    public ObservableCollection<PresetOverrideItemViewModel> Overrides { get; } = [];

    [ObservableProperty]
    private PresetOption? _selectedDefaultPreset;

    /// <summary>Null represents the "None" option.</summary>
    public string? SelectedPresetId => SelectedDefaultPreset is { Id: { Length: > 0 } id } ? id : null;

    public bool HasPresets => PresetOptions.Count > 0;
    public bool HasOverrides => Overrides.Count > 0;

    public DevicePresetSettingsViewModel(
        ICarConfigService carConfigService,
        Func<string, PresetOverride> createOverride,
        Action onChanged)
    {
        _carConfigService = carConfigService;
        _createOverride = createOverride;
        _onChanged = onChanged;
    }

    public void Initialize(
        IEnumerable<PresetOption> presets,
        string? defaultPresetId,
        IEnumerable<PresetOverride> overrides)
    {
        _isRefreshing = true;

        DefaultPresetOptions.Clear();
        PresetOptions.Clear();
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
    }

    partial void OnSelectedDefaultPresetChanged(PresetOption? value)
    {
        OnPropertyChanged(nameof(SelectedPresetId));
        if (!_isRefreshing)
        {
            _onChanged();
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
            _createOverride);
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
            RemoveOverride));

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
            RemoveOverride));
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
        Action<PresetOverrideItemViewModel> remove)
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
