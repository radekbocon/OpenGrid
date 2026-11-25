using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using OpenGrid.Controls;

namespace OpenGrid.ViewModels;

/// <summary>
/// Shared preset list/editor flow. Type-specific pages only provide the
/// persistence hooks and the editor view model.
/// </summary>
public abstract partial class PresetListViewModel<TPreset> : ViewModelBase where TPreset : class
{
    public ObservableCollection<PresetItemViewModel<TPreset>> Presets { get; } = [];

    public bool HasPresets => Presets.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPresetSelected))]
    private PresetItemViewModel<TPreset>? _selectedPreset;

    [ObservableProperty]
    private object? _editor;

    public bool IsPresetSelected => SelectedPreset is not null;

    protected abstract IReadOnlyList<TPreset> GetPresets();
    protected abstract TPreset CreatePreset(string name, TPreset? copyFrom);
    protected abstract void SavePreset(TPreset preset);
    protected abstract void RemovePreset(TPreset preset);
    protected abstract string GetPresetName(TPreset preset);
    protected abstract void SetPresetName(TPreset preset, string name);
    protected abstract string GetPresetSummary(TPreset preset);
    protected abstract object? CreateEditor(TPreset preset);
    protected abstract PresetItemViewModel<TPreset> CreateItem(TPreset preset);

    /// <summary>Loads presets once when the page view model is created.</summary>
    public void InitializePresets()
    {
        Refresh();
    }

    partial void OnSelectedPresetChanged(PresetItemViewModel<TPreset>? value)
    {
        Editor = value is null ? null : CreateEditor(value.Preset);
    }

    private void Refresh()
    {
        Presets.Clear();
        foreach (var preset in GetPresets().OrderBy(GetPresetName, StringComparer.OrdinalIgnoreCase))
        {
            Presets.Add(CreateItem(preset));
        }

        OnPropertyChanged(nameof(HasPresets));
        SelectedPreset = Presets.FirstOrDefault();
    }

    [RelayCommand]
    private void AddPreset()
    {
        AddAndSelect(CreatePreset(GetUniqueName("New Preset"), null));
    }

    [RelayCommand]
    private void DuplicatePreset()
    {
        if (SelectedPreset is not { } selected)
        {
            return;
        }

        AddAndSelect(CreatePreset(
            GetUniqueName($"{GetPresetName(selected.Preset)} Copy"),
            selected.Preset));
    }

    [RelayCommand]
    private async Task RemovePresetAsync()
    {
        if (SelectedPreset is not { } selected)
        {
            return;
        }

        var confirmDialog = new ConfirmDialog($"Delete preset '{GetPresetName(selected.Preset)}'?");
        await DialogHost.Show(confirmDialog);

        if (!confirmDialog.Result)
        {
            return;
        }

        var index = Presets.IndexOf(selected);
        RemovePreset(selected.Preset);
        Presets.Remove(selected);

        OnPropertyChanged(nameof(HasPresets));
        SelectedPreset = Presets.Count == 0
            ? null
            : Presets[Math.Clamp(index, 0, Presets.Count - 1)];
    }

    protected void OnEditorChanged()
    {
        if (SelectedPreset is not { } selected)
        {
            return;
        }

        SavePreset(selected.Preset);
        selected.RefreshSummary();
    }

    private void AddAndSelect(TPreset preset)
    {
        var item = CreateItem(preset);
        Presets.Add(item);
        OnPropertyChanged(nameof(HasPresets));
        SelectedPreset = item;
    }

    private string GetUniqueName(string baseName)
    {
        if (!GetPresets().Any(x =>
                string.Equals(GetPresetName(x), baseName, StringComparison.OrdinalIgnoreCase)))
        {
            return baseName;
        }

        for (var suffix = 2; ; suffix++)
        {
            var candidate = $"{baseName} {suffix}";
            if (!GetPresets().Any(x =>
                    string.Equals(GetPresetName(x), candidate, StringComparison.OrdinalIgnoreCase)))
            {
                return candidate;
            }
        }
    }
}
