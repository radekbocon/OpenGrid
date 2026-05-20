using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using SimLab.Models;

namespace SimLab.Services.SharedMemory;

public class DirtRallyTelemetryClient : ITelemetryClient
{
    private const int DefaultPort = 20777;
    private static readonly int PacketSize = Marshal.SizeOf<DirtRallyUdpData>();
    private static readonly int MinimumPacketSize = 256;
    private static readonly IPAddress BroadcastAddress = IPAddress.Parse("127.0.0.1");

    private UdpClient? _udpClient;
    private bool _connected;

    public int Port { get; }

    public DirtRallyTelemetryClient() : this(DefaultPort)
    {
    }

    public DirtRallyTelemetryClient(int port)
    {
        Port = port;
    }

    public Task<bool> ConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            _udpClient = new UdpClient(new IPEndPoint(BroadcastAddress, Port));
            _connected = true;
            Log.Information("DirtRallyTelemetryClient: Listening on UDP port {Port}", Port);
            return Task.FromResult(true);
        }
        catch (Exception e)
        {
            Log.Error(e, "DirtRallyTelemetryClient: Failed to bind to UDP port {Port}", Port);
            _connected = false;
            return Task.FromResult(false);
        }
    }

    public void Stop()
    {
        _connected = false;
        if (_udpClient == null)
            return;

        try
        {
            _udpClient.Close();
            _udpClient.Dispose();
        }
        catch (Exception e)
        {
            Log.Error(e, "DirtRallyTelemetryClient: Error closing UDP client");
        }

        _udpClient = null;
    }

    public TelemetryRecord? ReadTelemetry()
    {
        if (!_connected || _udpClient == null)
            return null;

        try
        {
            if (_udpClient.Available < MinimumPacketSize)
                return null;

            var remoteEp = new IPEndPoint(BroadcastAddress, Port);
            var data = _udpClient.Receive(ref remoteEp);

            if (data.Length < MinimumPacketSize)
                return null;

            // Drain stale packets to get the latest
            while (_udpClient.Available >= MinimumPacketSize)
            {
                data = _udpClient.Receive(ref remoteEp);
                if (data.Length < MinimumPacketSize)
                    return null;
            }

            if (IsResetPacket(data))
                return null;

            var buffer = data.Length >= PacketSize ? data : PadToStructSize(data);
            var packet = MemoryMarshal.Read<DirtRallyUdpData>(buffer.AsSpan());

            return new TelemetryRecord
            {
                Timestamp = DateTime.UtcNow,
                SpeedKmh = packet.Speed * 3.6f,
                Gas = packet.Throttle,
                Brake = packet.Brake,
                Clutch = packet.Clutch,
                SteerAngle = packet.Steering,
                CurrentGear = MapGear((int)packet.Gear),
                EngineRpm = packet.EngineRPM * 10f,
                MaxRpm = packet.MaxRPM * 10f,
                LapTime = TimeSpan.FromSeconds(packet.LapTime),
                LastLapTime = TimeSpan.FromSeconds(packet.LastLapTime),
                CurrentLap = (int)packet.Lap,
                Distance = packet.Distance,
                Position = (int)packet.RacePos,
                SessionType = SessionType.Race,
                TirePressures = new TireValues(packet.TirePressureFL, packet.TirePressureFR, packet.TirePressureRL, packet.TirePressureRR)
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "DirtRallyTelemetryClient: Error reading telemetry");
            return null;
        }
    }

    private static byte[] PadToStructSize(byte[] data)
    {
        var padded = new byte[PacketSize];
        Array.Copy(data, padded, data.Length);
        return padded;
    }

    private static Gear MapGear(int gear)
    {
        return gear switch
        {
            -1 => Gear.R,
            0 => Gear.N,
            1 => Gear.N1,
            2 => Gear.N2,
            3 => Gear.N3,
            4 => Gear.N4,
            5 => Gear.N5,
            >= 6 => Gear.N6,
            _ => Gear.N,
        };
    }

    private static bool IsResetPacket(byte[] data)
    {
        for (var i = 0; i < data.Length; i++)
        {
            if (data[i] != 0)
                return false;
        }
        return true;
    }
}
