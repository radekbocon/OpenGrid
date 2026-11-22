namespace OpenGrid.Models;

/// <summary>
/// One complete LED frame shown at a given RPM threshold. Each entry in
/// <see cref="Leds"/> describes one physical LED: a colour, or black ("[]" /
/// "#000000") for off.
/// </summary>
public sealed class RpmLedSegment
{
    /// <summary>The wheel telemetry bitmask supports 16 RPM LEDs.</summary>
    public const int MaxLeds = 16;

    public const string OffLed = "[]";

    public int ThresholdPercent { get; set; } = 100;

    /// <summary>Blink this whole frame on/off while it is active.</summary>
    public bool Flicker { get; set; }

    public List<string> Leds { get; set; } = [];

    public RpmLedSegment()
    {
    }

    public RpmLedSegment(IEnumerable<string?> leds, int thresholdPercent, bool flicker = false)
    {
        Leds = leds.Select(x => x ?? OffLed).ToList();
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

    public static bool IsOff(string? value)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text) ||
            text is "[]" or "[ ]" or "-" or "." or "_" or "off" or "0" or "black")
        {
            return true;
        }

        var hex = text.TrimStart('#');
        if (hex.Length == 3)
        {
            hex = string.Concat(hex.Select(c => $"{c}{c}"));
        }
        else if (hex.Length == 8)
        {
            hex = hex[..6];
        }

        // A black LED is physically indistinguishable from an off LED.
        return hex.Equals("000000", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Parses one LED cell. Returns <see langword="null"/> for an off cell.
    /// Single-letter codes G/R/Y/B/O/P/C/W are accepted alongside the longer
    /// colour names and #RRGGBB hex values.
    /// </summary>
    public static (byte R, byte G, byte B)? TryParseLed(string? value) =>
        IsOff(value) ? null : ParseColor(value);

    public static (byte R, byte G, byte B) ParseColor(string? value)
    {
        var text = value?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return (0, 255, 0);
        }

        switch (text.ToLowerInvariant())
        {
            case "g":
            case "green": return (0, 255, 0);
            case "r":
            case "red": return (255, 0, 0);
            case "y":
            case "yellow": return (255, 255, 0);
            case "b":
            case "blue": return (0, 0, 255);
            case "o":
            case "orange": return (255, 128, 0);
            case "p":
            case "purple": return (128, 0, 255);
            case "c":
            case "cyan": return (0, 255, 255);
            case "w":
            case "white": return (255, 255, 255);
        }

        var hex = text.TrimStart('#');
        if (hex.Length == 3)
        {
            hex = string.Concat(hex.Select(c => $"{c}{c}"));
        }

        if (hex.Length == 8)
        {
            hex = hex[..6];
        }

        if (hex.Length == 6 &&
            byte.TryParse(hex[..2], System.Globalization.NumberStyles.HexNumber, null, out var r) &&
            byte.TryParse(hex[2..4], System.Globalization.NumberStyles.HexNumber, null, out var g) &&
            byte.TryParse(hex[4..6], System.Globalization.NumberStyles.HexNumber, null, out var b))
        {
            return (r, g, b);
        }

        return (0, 255, 0);
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

    public static uint BuildMask(IEnumerable<string?> leds)
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
    public static byte[] BuildColors(IEnumerable<string?> leds, int totalLedCount)
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

            if (TryParseLed(led) is { } rgb)
            {
                colors[index * 3] = rgb.R;
                colors[index * 3 + 1] = rgb.G;
                colors[index * 3 + 2] = rgb.B;
            }

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

        int green;
        int yellow;
        int blue;

        if (total < 3)
        {
            green = 1;
            yellow = total >= 2 ? 1 : 0;
            blue = total >= 3 ? 1 : 0;
        }
        else
        {
            green = Math.Max(1, total / 3);
            yellow = Math.Max(1, (total - green) / 2);
            blue = total - green - yellow;
            if (blue < 1)
            {
                blue = 1;
                yellow = Math.Max(1, total - green - blue);
            }
        }

        return
        [
            new RpmLedSegment(Frame(total, (green, "G")), 80),
            new RpmLedSegment(Frame(total, (green, "G"), (yellow, "Y")), 90),
            new RpmLedSegment(Frame(total, (green, "G"), (yellow, "Y"), (blue, "B")), 95),
            new RpmLedSegment(Enumerable.Repeat("B", total), 100, true),
        ];
    }

    private static List<string> Frame(int total, params (int Count, string Color)[] parts)
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

/// <summary>
/// A device connected through a serial port. Currently used for MOZA Racing
/// wheelbases and other MOZA devices that expose the internal bus over USB CDC.
/// </summary>
public class SerialDevice : IDevice
{
    public required string Id { get; init; }
    public DeviceType DeviceType { get; init; } = DeviceType.Serial;
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
    public MozaDeviceCategory Category { get; init; }

    public DeviceConnectionStatus Status { get; set; }

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
