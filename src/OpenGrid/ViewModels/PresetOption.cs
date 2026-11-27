using OpenGrid.Models;

namespace OpenGrid.ViewModels;

/// <summary>
/// Shared combo-box option for default presets and overrides.
/// </summary>
public sealed class PresetOption
{
    public static PresetOption None { get; } = new(null, "None");

    public PresetOption(string? id, string name)
    {
        Id = id ?? string.Empty;
        Name = string.IsNullOrWhiteSpace(name) ? "None" : name;
    }

    public PresetOption(BassShakerPreset preset) : this(preset.Id, preset.Name)
    {
    }

    public PresetOption(LedPreset preset) : this(preset.Id, preset.Name)
    {
    }

    public string Id { get; }
    public string Name { get; }

    public override string ToString() => Name;
}
