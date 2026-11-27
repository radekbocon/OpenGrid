namespace OpenGrid.Models;

/// <summary>
/// Effect types that can be added to an LED preset. Additional types can be
/// added without changing how presets are stored, edited or assigned.
/// </summary>
public enum LedEffectType
{
    Rpm,
}

/// <summary>
/// Device-agnostic LED effect configuration. LED count is authoring metadata;
/// frames are normalized to the physical LED count of the target device when
/// an effect is evaluated.
/// </summary>
public class LedPresetConfiguration
{
    private List<LedEffectSettings> _effects = [];

    public int LedCount { get; set; } = 10;

    public List<LedEffectSettings> Effects
    {
        get => _effects;
        set => _effects = value ?? [];
    }

    public LedPresetConfiguration Clone()
    {
        return new LedPresetConfiguration
        {
            LedCount = LedCount,
            Effects = Effects
                .Where(static effect => effect is not null)
                .Select(static effect => effect.Clone())
                .ToList(),
        };
    }
}

public class LedPreset
{
    private LedPresetConfiguration _configuration = new();

    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public LedPresetConfiguration Configuration
    {
        get => _configuration;
        set => _configuration = value ?? new LedPresetConfiguration();
    }
}

public class LedPresetOverride : PresetOverride
{
}
