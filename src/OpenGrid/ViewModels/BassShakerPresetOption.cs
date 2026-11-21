using OpenGrid.Models;

namespace OpenGrid.ViewModels;

public sealed class BassShakerPresetOption
{
    public static BassShakerPresetOption None { get; } = new(null);

    public BassShakerPresetOption(BassShakerPreset? preset)
    {
        Preset = preset;
    }

    public BassShakerPreset? Preset { get; }
    public string Id => Preset?.Id ?? string.Empty;
    public string Name => Preset?.Name ?? "None";

    public override string ToString() => Name;
}
