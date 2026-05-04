using System;
using System.IO.Ports;

namespace SimLab.Services.Devices.Moza;

public static class MozaProtocol
{
    public const byte StartByte = 0x7E;
    public const byte GroupLedControl = 0x3F;
    public const byte DeviceWheel = 0x17;
    public const byte CmdRpmLedTelemetry = 0x1A;

    public static byte CalculateChecksum(byte[] frame)
    {
        uint sum = 0x0D;
        foreach (var b in frame)
        {
            sum += b;
        }

        return (byte)(sum % 256);
    }

    public static byte[] BuildRpmLedFrame(ushort currentRpm, ushort maxRpm)
    {
        var rpmFraction = maxRpm > 0 ? (ushort)(currentRpm * 1023 / maxRpm) : (ushort)0;

        var payload = new byte[]
        {
            CmdRpmLedTelemetry, 0x00,
            (byte)(rpmFraction & 0xFF), (byte)((rpmFraction >> 8) & 0xFF),
            0x00, 0x00,
            0xFF, 0x03,
            0x00, 0x00
        };

        return BuildFrame(GroupLedControl, DeviceWheel, payload);
    }

    public static byte[] BuildFrame(byte group, byte device, byte[] payload)
    {
        var frame = new byte[4 + payload.Length + 1];
        frame[0] = StartByte;
        frame[1] = (byte)(payload.Length + 2);
        frame[2] = group;
        frame[3] = device;
        Buffer.BlockCopy(payload, 0, frame, 4, payload.Length);
        frame[frame.Length - 1] = CalculateChecksum(frame[..^1]);
        return frame;
    }
}
