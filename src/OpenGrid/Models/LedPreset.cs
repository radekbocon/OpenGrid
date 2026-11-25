using Avalonia.Media;

namespace OpenGrid.Models;

/// <summary>
/// Effect currently stored in an LED preset. Additional effect types can be
/// added without changing how presets are stored, edited or assigned.
/// </summary>
public enum LedEffectType
{
    Rpm,
}

/// <summary>
/// Device-agnostic LED effect configuration. LED count is authoring metadata;
/// frames are normalized to the physical LED count of the target device when
/// the effect is evaluated.
/// </summary>
public class LedPresetConfiguration
{
    public LedEffectType EffectType { get; set; } = LedEffectType.Rpm;

    public int LedCount { get; set; } = 10;

    public List<RpmLedSegment> RpmSegments { get; set; } = [];

    public LedPresetConfiguration Clone()
    {
        return new LedPresetConfiguration
        {
            EffectType = EffectType,
            LedCount = LedCount,
            RpmSegments = RpmSegments
                .Where(static segment => segment is not null)
                .Select(static segment => new RpmLedSegment
                {
                    ThresholdPercent = segment.ThresholdPercent,
                    Flicker = segment.Flicker,
                    Leds = segment.Leds.Select(static led => new Color(
                        led.A, led.R, led.G, led.B)).ToList(),
                })
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
