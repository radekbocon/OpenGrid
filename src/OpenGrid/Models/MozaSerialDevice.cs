using Avalonia.Media;

namespace OpenGrid.Models;

/// <summary>
/// A MOZA Racing serial device with RPM LED telemetry support.
/// </summary>
public class MozaSerialDevice : ISerialDevice
{
    public required string Id { get; init; }
    public DeviceType DeviceType { get; init; } = DeviceType.MozaSerial;
    public required string Name { get; init; }
    public string? Description { get; set; }
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Stable path to the port (/dev/serial/by-id/... when available).
    /// </summary>
    public required string PortName { get; init; }

    public string? SerialNumber { get; init; }
    public ushort VendorId { get; init; }
    public ushort ProductId { get; init; }

    public DeviceConnectionStatus Status { get; set; }

    public MozaDeviceCategory Category { get; init; }

    /// <summary>
    /// Drive the wheel RPM LEDs from game telemetry (requires the global
    /// setting to be enabled as well).
    /// </summary>
    public bool DriveLedsWithTelemetry { get; set; } = true;

    /// <summary>
    /// Number of physical LEDs in one RPM frame.
    /// </summary>
    public int RpmLedCount { get; set; } = 10;

    /// <summary>
    /// Complete LED frames, ordered by threshold. Each frame spans all
    /// <see cref="RpmLedCount"/> LEDs.
    /// </summary>
    public List<RpmLedSegment> RpmSegments { get; set; } = [];

    /// <summary>
    /// Whether this device should actively drive RPM LEDs from telemetry.
    /// </summary>
    public bool IsRpmLedActive()
    {
        if (!IsEnabled || !DriveLedsWithTelemetry)
        {
            return false;
        }

        // Wheel LED commands are routed to the wheelbase / hub serial port.
        return Category is MozaDeviceCategory.WheelBase
            or MozaDeviceCategory.Wheel
            or MozaDeviceCategory.Hub
            or MozaDeviceCategory.Unknown;
    }

    public List<RpmLedSegment> GetNormalizedRpmSegments()
    {
        return RpmLedSegment.Normalize(RpmSegments, RpmLedCount);
    }
}

public enum MozaDeviceCategory
{
    Unknown,
    WheelBase,
    Wheel,
    Pedals,
    Dashboard,
    Shifter,
    Handbrake,
    Hub,
    Other,
}

/// <summary>
/// One complete LED frame shown at a given RPM threshold. Each entry in
/// <see cref="Leds"/> describes one physical LED: a colour, or black ("[]" /
/// "#000000") for off.
/// </summary>
public sealed class RpmLedSegment
{
    /// <summary>The wheel telemetry bitmask supports 16 RPM LEDs.</summary>
    public const int MaxLeds = 16;

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

    public void Resize(int totalLedCount)
    {
        var count = Math.Clamp(totalLedCount, 1, MaxLeds);
        while (Leds.Count < count)
        {
            Leds.Add(OffLed);
        }

        if (Leds.Count > count)
        {
            Leds.RemoveRange(count, Leds.Count - count);
        }
    }

    public static bool IsOff(Color value)
    {
        return value == OffLed;
    }

    /// <summary>
    /// Orders frames by threshold and resizes every frame to the configured
    /// number of physical LEDs.
    /// </summary>
    public static List<RpmLedSegment> Normalize(
        IEnumerable<RpmLedSegment>? segments,
        int totalLedCount)
    {
        var count = Math.Clamp(totalLedCount, 1, MaxLeds);
        var source = segments?.OrderBy(x => x.ThresholdPercent).ToList();
        if (source is null || source.Count == 0)
        {
            return CreateDefault(count);
        }

        return source
            .Select(segment =>
            {
                var copy = new RpmLedSegment
                {
                    ThresholdPercent = Math.Clamp(segment.ThresholdPercent, 1, 100),
                    Flicker = segment.Flicker,
                    Leds = Enumerable.Range(0, count)
                        .Select(i => i < segment.Leds.Count ? segment.Leds[i] : OffLed)
                        .ToList(),
                };
                return copy;
            })
            .ToList();
    }

    public static uint BuildMask(IEnumerable<Color> leds)
    {
        uint mask = 0;
        var index = 0;

        foreach (var led in leds)
        {
            if (index >= MaxLeds)
            {
                break;
            }

            if (!IsOff(led))
            {
                mask |= 1u << index;
            }

            index++;
        }

        return mask;
    }

    /// <summary>
    /// Converts one frame into the RGB triplets expected by the wheel's live
    /// colour chunks. Off LEDs are sent as black.
    /// </summary>
    public static byte[] BuildColors(IEnumerable<Color> leds, int totalLedCount)
    {
        var count = Math.Clamp(totalLedCount, 1, MaxLeds);
        var colors = new byte[count * 3];
        var index = 0;

        foreach (var led in leds)
        {
            if (index >= count)
            {
                break;
            }

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
    public static List<RpmLedSegment> CreateDefault(int totalLedCount)
    {
        var total = Math.Clamp(totalLedCount, 1, MaxLeds);

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
