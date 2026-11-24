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
    /// Default frames matching the requested style: green, then yellow, then
    /// blue, then an all-blue blinking redline frame.
    /// </summary>
    public static List<RpmLedSegment> CreateDefault(int total)
    {
        int greenCount;
        int yellowCount;
        int blueCount;

        if (total < 3)
        {
            greenCount = 1;
            yellowCount = total >= 2 ? 1 : 0;
            blueCount = total >= 3 ? 1 : 0;
        }
        else
        {
            greenCount = Math.Max(1, total / 3);
            yellowCount = Math.Max(1, (total - greenCount) / 2);
            blueCount = total - greenCount - yellowCount;
            if (blueCount < 1)
            {
                blueCount = 1;
                yellowCount = Math.Max(1, total - greenCount - blueCount);
            }
        }

        return
        [
            new RpmLedSegment(Frame(total, (greenCount, Colors.Green)), 80),
            new RpmLedSegment(Frame(total, (greenCount, Colors.Green), (yellowCount, Colors.Yellow)), 90),
            new RpmLedSegment(Frame(total, (greenCount, Colors.Green), (yellowCount, Colors.Yellow), (blueCount, Colors.Red)), 95),
            new RpmLedSegment(Enumerable.Repeat(Colors.Red, total), 100, true),
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