namespace OpenGrid.ViewModels;

/// <summary>
/// Hosts the preset pages. Each tab owns its own preset type, so new LED
/// effect types can be added as extra tabs later.
/// </summary>
public sealed class PresetsViewModel : ViewModelBase
{
    public BassShakerPresetsViewModel BassShakers { get; }
    public LedPresetsViewModel LedPresets { get; }

    public PresetsViewModel(
        BassShakerPresetsViewModel bassShakers,
        LedPresetsViewModel ledPresets)
    {
        BassShakers = bassShakers;
        LedPresets = ledPresets;
        IsMenuItem = true;
    }
}
