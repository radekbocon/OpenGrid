using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenGrid.ViewModels;

/// <summary>
/// Shared list item for preset pages. Renaming is validated here so every
/// preset type behaves the same way.
/// </summary>
public partial class PresetItemViewModel<TPreset> : ViewModelBase where TPreset : class
{
    private readonly Action<TPreset> _save;
    private readonly Func<TPreset, string> _getSummary;

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

    public PresetItemViewModel(
        TPreset preset,
        Action<TPreset> save,
        Func<TPreset, string> getName,
        Action<TPreset, string> setName,
        Func<TPreset, string> getSummary)
    {
        Preset = preset;
        _save = save;
        GetName = getName;
        SetName = setName;
        _getSummary = getSummary;
    }

    private Func<TPreset, string> GetName { get; }
    private Action<TPreset, string> SetName { get; }

    public void RefreshSummary() => OnPropertyChanged(nameof(Summary));
}
