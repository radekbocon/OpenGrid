using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OpenGrid.ViewModels;

/// <summary>
/// Shared list item for preset pages. Renaming is validated here so every
/// preset type behaves the same way.
/// </summary>
public partial class PresetItemViewModel<TPreset> : ViewModelBase where TPreset : class
{
    private readonly Action<TPreset> _save;
    private readonly Func<TPreset, string> _getSummary;
    private readonly Action<PresetItemViewModel<TPreset>> _duplicate;
    private readonly Func<PresetItemViewModel<TPreset>, Task> _remove;

    public TPreset Preset { get; }

    public string Name
    {
        get => GetName(Preset);
        set
        {
            if (string.Equals(GetName(Preset), value, StringComparison.Ordinal))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                OnPropertyChanged();
                return;
            }

            SetName(Preset, value.Trim());
            _save(Preset);
            OnPropertyChanged();
            RefreshSummary();
        }
    }

    public string Summary => _getSummary(Preset);

    [RelayCommand]
    private void Duplicate() => _duplicate(this);

    [RelayCommand]
    private Task RemoveAsync() => _remove(this);

    public PresetItemViewModel(
        TPreset preset,
        Action<TPreset> save,
        Func<TPreset, string> getName,
        Action<TPreset, string> setName,
        Func<TPreset, string> getSummary,
        Action<PresetItemViewModel<TPreset>> duplicate,
        Func<PresetItemViewModel<TPreset>, Task> remove)
    {
        Preset = preset;
        _save = save;
        GetName = getName;
        SetName = setName;
        _getSummary = getSummary;
        _duplicate = duplicate;
        _remove = remove;
    }

    private Func<TPreset, string> GetName { get; }
    private Action<TPreset, string> SetName { get; }

    public void RefreshSummary() => OnPropertyChanged(nameof(Summary));
}
