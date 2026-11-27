using Avalonia.Media;

namespace OpenGrid.Models;

/// <summary>
/// One effect inside an LED preset. Effects are stored and evaluated in list
/// order, so later effects can override LEDs set by earlier ones.
/// </summary>
public class LedEffectSettings
{
    private List<RpmLedSegment> _rpmSegments = [];

    public LedEffectType Type { get; set; } = LedEffectType.Rpm;

    public bool IsEnabled { get; set; } = true;

    public List<RpmLedSegment> RpmSegments
    {
        get => _rpmSegments;
        set => _rpmSegments = value ?? [];
    }

    public LedEffectSettings Clone()
    {
        return new LedEffectSettings
        {
            Type = Type,
            IsEnabled = IsEnabled,
            RpmSegments = RpmSegments
                .Where(static segment => segment is not null)
                .Select(static segment => new RpmLedSegment
                {
                    ThresholdPercent = segment.ThresholdPercent,
                    Flicker = segment.Flicker,
                    Leds = segment.Leds
                        .Select(static led => new Color(led.A, led.R, led.G, led.B))
                        .ToList(),
                })
                .ToList(),
        };
    }
}

public static class LedEffectTypeExtensions
{
    public static string GetDisplayName(this LedEffectType type) => type switch
    {
        LedEffectType.Rpm => "Segmented RPM",
        _ => type.ToString(),
    };
}
