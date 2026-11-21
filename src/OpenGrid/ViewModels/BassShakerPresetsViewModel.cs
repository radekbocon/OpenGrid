using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using OpenGrid.Controls;
using OpenGrid.Services.Sound;

namespace OpenGrid.ViewModels;

public partial class BassShakerPresetsViewModel : ViewModelBase
{
    private readonly IBassShakerPresetService _presetService;

    public ObservableCollection<BassShakerPresetItemViewModel> Presets { get; } = [];

    public bool HasPresets => Presets.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPresetSelected))]
    public partial BassShakerPresetItemViewModel? SelectedPreset { get; set; }

    [ObservableProperty]
    public partial BassShakerConfigurationViewModel? Editor { get; set; }

    public bool IsPresetSelected => SelectedPreset is not null;

    public BassShakerPresetsViewModel(IBassShakerPresetService presetService)
    {
        _presetService = presetService;
        IsMenuItem = true;
    }

    protected override Task OnLoadedAsync()
    {
        Refresh();
        return base.OnLoadedAsync();
    }

    partial void OnSelectedPresetChanged(BassShakerPresetItemViewModel? value)
    {
        Editor = value is null
            ? null
            : new BassShakerConfigurationViewModel(value.Preset.Configuration, OnEditorChanged);
    }

    private void Refresh()
    {
        Presets.Clear();
        foreach (var preset in _presetService.GetAll().OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            Presets.Add(new BassShakerPresetItemViewModel(preset, _presetService));
        }

        OnPropertyChanged(nameof(HasPresets));
        SelectedPreset = Presets.FirstOrDefault();
    }

    [RelayCommand]
    private void AddPreset()
    {
        var preset = _presetService.Create(GetUniqueName("New Preset"));
        var item = new BassShakerPresetItemViewModel(preset, _presetService);
        Presets.Add(item);
        OnPropertyChanged(nameof(HasPresets));
        SelectedPreset = item;
    }

    [RelayCommand]
    private void DuplicatePreset()
    {
        if (SelectedPreset is not { } selected)
        {
            return;
        }

        var preset = _presetService.Create(
            GetUniqueName($"{selected.Preset.Name} Copy"),
            selected.Preset.Configuration);
        var item = new BassShakerPresetItemViewModel(preset, _presetService);
        Presets.Add(item);
        OnPropertyChanged(nameof(HasPresets));
        SelectedPreset = item;
    }

    [RelayCommand]
    private async Task RemovePresetAsync()
    {
        if (SelectedPreset is not { } selected)
        {
            return;
        }

        var confirmDialog = new ConfirmDialog($"Delete preset '{selected.Preset.Name}'?");
        await DialogHost.Show(confirmDialog);

        if (!confirmDialog.Result)
        {
            return;
        }

        var index = Presets.IndexOf(selected);
        _presetService.Remove(selected.Preset.Id);
        Presets.Remove(selected);

        OnPropertyChanged(nameof(HasPresets));
        SelectedPreset = Presets.Count == 0
            ? null
            : Presets[Math.Clamp(index, 0, Presets.Count - 1)];
    }

    private void OnEditorChanged()
    {
        if (SelectedPreset is not { } selected)
        {
            return;
        }

        _presetService.Save(selected.Preset);
        selected.RefreshSummary();
    }

    private string GetUniqueName(string baseName)
    {
        if (!_presetService.GetAll().Any(x =>
                string.Equals(x.Name, baseName, StringComparison.OrdinalIgnoreCase)))
        {
            return baseName;
        }

        for (var suffix = 2; ; suffix++)
        {
            var candidate = $"{baseName} {suffix}";
            if (!_presetService.GetAll().Any(x =>
                    string.Equals(x.Name, candidate, StringComparison.OrdinalIgnoreCase)))
            {
                return candidate;
            }
        }
    }
}
