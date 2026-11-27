using Avalonia.Media;

namespace OpenGrid.Models;

public sealed class RpmLedSegment
{
    public static readonly Color OffLed = new(255, 0, 0, 0);

    public int ThresholdPercent { get; set; } = 100;

    /// <summary>Blink this whole frame on/off while it is active.</summary>
    public bool Flicker { get; set; }

    public List<Color> Leds { get; set; } = [];

    public RpmLedSegment()
    {
    }

    public RpmLedSegment(IEnumerable<Color> leds, int thresholdPercent, bool flicker = false)
    {
        Leds = leds.ToList();
        ThresholdPercent = thresholdPercent;
        Flicker = flicker;
    }

    private static bool IsOff(Color value)
    {
        return value == OffLed;
    }

    /// <summary>
    /// Orders frames by threshold and resizes every frame to the configured
    /// number of physical LEDs.
    /// </summary>
    public static List<RpmLedSegment> Normalize(IEnumerable<RpmLedSegment>? segments,
        int totalLedCount)
    {
        var source = segments?.OrderBy(x => x.ThresholdPercent).ToList();
        if (source is null || source.Count == 0)
        {
            return CreateDefault(totalLedCount);
        }

        return source
            .Select(segment =>
            {
                var copy = new RpmLedSegment
                {
                    ThresholdPercent = Math.Clamp(segment.ThresholdPercent, 1, 100),
                    Flicker = segment.Flicker,
                    Leds = Enumerable.Range(0, totalLedCount)
                        .Select(i => i < segment.Leds.Count ? segment.Leds[i] : OffLed)
                        .ToList(),
                };
                return copy;
            })
            .ToList();
    }

    /// <summary>
    /// Converts one frame into the RGB triplets expected by the wheel's live
    /// colour chunks. Off LEDs are sent as black.
    /// </summary>
    public static byte[] BuildColors(IEnumerable<Color> leds, int count)
    {
        var colors = new byte[count * 3];
        var index = 0;

        foreach (var led in leds)
        {
            colors[index * 3] = led.R;
            colors[index * 3 + 1] = led.G;
            colors[index * 3 + 2] = led.B;

            index++;
        }

        return colors;
    }

    /// <summary>
    /// Default three-segment shift light: green, then amber, then red, followed
    /// by an all-red blinking redline frame. The outer segments are the same
    /// size and the amber segment takes the remaining LEDs, so 10 LEDs split
    /// 3 / 4 / 3.
    /// </summary>
    public static List<RpmLedSegment> CreateDefault(int total)
    {
        total = Math.Max(1, total);

        var outerCount = (total + 1) / 3;
        var amberCount = Math.Max(0, total - outerCount * 2);

        return
        [
            new RpmLedSegment(Frame(total, (outerCount, LedColorPalette.Green)), 80),
            new RpmLedSegment(Frame(total, (outerCount, LedColorPalette.Green), (amberCount, LedColorPalette.Amber)), 90),
            new RpmLedSegment(Frame(total, (outerCount, LedColorPalette.Green), (amberCount, LedColorPalette.Amber), (outerCount, LedColorPalette.Red)), 95),
            new RpmLedSegment(Enumerable.Repeat(LedColorPalette.Red, total), 100, true),
        ];
    }

    private static List<Color> Frame(int total, params (int Count, Color Color)[] parts)
    {
        var frame = Enumerable.Repeat(OffLed, total).ToList();
        var offset = 0;

        foreach (var (count, color) in parts)
        {
            for (var i = 0; i < count && offset < total; i++)
            {
                frame[offset++] = color;
            }
        }

        return frame;
    }
}