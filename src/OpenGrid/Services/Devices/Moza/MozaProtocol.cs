using System.Buffers.Binary;

namespace OpenGrid.Services.Devices.Moza;

/// <summary>
/// MOZA serial bus wire protocol.
///
/// Frame layout: 7E [N] [group] [device] [payload] [checksum], where N is the
/// payload byte count only. Command values are big-endian, except for LED
/// bitmasks which are little-endian.
///
/// The framing, 115200 baud rate and checksum magic value follow the boxflat
/// implementation (boxflat/moza_command.py and data/serial.yml). The checksum
/// includes the frame start byte and a fixed magic value of 0x0D. Bytes 0x7E
/// in the body/checksum are escaped by doubling them on the wire; the checksum
/// is calculated over the escaped representation, as documented in boxflat PR
/// #131.
/// </summary>
public static class MozaProtocol
{
    public const byte MessageStart = 0x7E;
    public const byte MagicValue = 0x0D;
    public const int BaudRate = 115200;
    public const int VendorId = 0x346E;

    public const byte WheelDeviceId = 0x17;
    public const byte WheelWriteGroup = 0x3F;
    public const byte WheelReadGroup = 0x40;

    public const byte WheelRpmColorsCommand = 0x19;
    public const byte WheelRpmBitmaskCommand = 0x1A;
    public const byte WheelRpmLedGroup = 0x00;

    public const byte WheelTelemetryModeCommand = 0x1C;
    public const byte WheelTelemetryModeActive = 0x01;

    /// <summary>
    /// Sum of all bytes preceding the checksum, including the start byte and
    /// the magic value, plus one extra 0x7E for every escaped body byte.
    /// </summary>
    public static byte CalculateChecksum(ReadOnlySpan<byte> frameWithoutChecksum)
    {
        var sum = (int)MagicValue;

        foreach (var value in frameWithoutChecksum)
        {
            sum += value;
        }

        // Escaping a 0x7E in the body puts two of them on the wire, and the
        // device calculates the checksum from the transmitted bytes.
        for (var i = 2; i < frameWithoutChecksum.Length; i++)
        {
            if (frameWithoutChecksum[i] == MessageStart)
            {
                sum += MessageStart;
            }
        }

        return (byte)(sum & 0xFF);
    }

    /// <summary>
    /// Builds a raw, unescaped frame. The returned bytes are ready to write to
    /// the serial port (0x7E bytes are escaped already).
    /// </summary>
    public static byte[] BuildFrame(byte group, byte device, ReadOnlySpan<byte> payload)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(payload.Length, byte.MaxValue);

        var frame = new byte[4 + payload.Length + 1];
        frame[0] = MessageStart;
        frame[1] = (byte)payload.Length;
        frame[2] = group;
        frame[3] = device;
        payload.CopyTo(frame.AsSpan(4, payload.Length));
        frame[^1] = CalculateChecksum(frame.AsSpan(0, frame.Length - 1));

        return EscapeFrame(frame);
    }

    /// <summary>
    /// Legacy 2-byte RPM bitmask used by older wheels and by boxflat's
    /// wheel-send-rpm-telemetry command.
    /// </summary>
    public static byte[] BuildWheelRpmLedFrameLegacy(ushort activeMask)
    {
        Span<byte> payload = stackalloc byte[4];
        payload[0] = WheelRpmBitmaskCommand;
        payload[1] = WheelRpmLedGroup;
        BinaryPrimitives.WriteUInt16LittleEndian(payload[2..], activeMask);

        return BuildFrame(WheelWriteGroup, WheelDeviceId, payload);
    }

    /// <summary>
    /// Writes a five-LED live colour chunk. Unused slots use index 0xFF; zero
    /// padding would be interpreted as "LED 0 = black" by the firmware.
    /// </summary>
    public static byte[] BuildWheelRpmColorChunkFrame(int startIndex, ReadOnlySpan<byte> colors)
    {
        Span<byte> payload = stackalloc byte[2 + 5 * 4];
        payload[0] = WheelRpmColorsCommand;
        payload[1] = WheelRpmLedGroup;

        for (var i = 0; i < 5; i++)
        {
            var offset = 2 + i * 4;
            var colorIndex = startIndex + i;
            if (colorIndex < colors.Length / 3)
            {
                payload[offset] = (byte)colorIndex;
                payload[offset + 1] = colors[colorIndex * 3];
                payload[offset + 2] = colors[colorIndex * 3 + 1];
                payload[offset + 3] = colors[colorIndex * 3 + 2];
            }
            else
            {
                payload[offset] = 0xFF;
                payload[offset + 1] = 0;
                payload[offset + 2] = 0;
                payload[offset + 3] = 0;
            }
        }

        return BuildFrame(WheelWriteGroup, WheelDeviceId, payload);
    }

    public static byte[] BuildWheelTelemetryModeReadFrame()
    {
        Span<byte> payload = stackalloc byte[3];
        payload[0] = WheelTelemetryModeCommand;
        payload[1] = WheelRpmLedGroup;
        payload[2] = 0;

        return BuildFrame(WheelReadGroup, WheelDeviceId, payload);
    }

    public static byte[] BuildWheelTelemetryModeWriteFrame(byte mode)
    {
        Span<byte> payload = stackalloc byte[3];
        payload[0] = WheelTelemetryModeCommand;
        payload[1] = WheelRpmLedGroup;
        payload[2] = mode;

        return BuildFrame(WheelWriteGroup, WheelDeviceId, payload);
    }

    public static byte ResponseGroup(byte group) => (byte)(group | 0x80);

    public static byte ResponseDevice(byte device) =>
        (byte)((device << 4) | (device >> 4));

    /// <summary>
    /// Escapes all 0x7E bytes starting at the group field. The frame start at
    /// index 0 is the only unescaped 0x7E.
    /// </summary>
    private static byte[] EscapeFrame(byte[] frame)
    {
        var escapedCount = 0;
        for (var i = 2; i < frame.Length; i++)
        {
            if (frame[i] == MessageStart)
            {
                escapedCount++;
            }
        }

        if (escapedCount == 0)
        {
            return frame;
        }

        var escaped = new byte[frame.Length + escapedCount];
        escaped[0] = frame[0];
        escaped[1] = frame[1];

        var index = 2;
        foreach (var value in frame.AsSpan(2))
        {
            escaped[index++] = value;
            if (value == MessageStart)
            {
                escaped[index++] = MessageStart;
            }
        }

        return escaped;
    }
}
