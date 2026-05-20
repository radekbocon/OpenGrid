using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using SimLab.Models;

namespace SimLab.Services.SharedMemory;

public class DirtRallyTelemetryClient : ITelemetryClient
{
    private const int DefaultPort = 20777;
    private const int PacketSize = 256;
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
            if (_udpClient.Available < PacketSize)
                return null;

            var remoteEp = new IPEndPoint(BroadcastAddress, Port);
            var data = _udpClient.Receive(ref remoteEp);

            if (data.Length < PacketSize)
                return null;

            // Drain stale packets to get the latest
            while (_udpClient.Available >= PacketSize)
            {
                data = _udpClient.Receive(ref remoteEp);
                if (data.Length < PacketSize)
                    return null;
            }

            if (IsResetPacket(data))
                return null;

            var floats = new float[64];
            for (var i = 0; i < 64; i++)
            {
                floats[i] = BitConverter.ToSingle(data, i * 4);
            }

            var speedKmh = floats[7] * 3.6f;
            var gearRaw = (int)Math.Round(floats[33]);
            var engineRpm = floats[37] * 10f;
            var maxRpm = floats[63] * 10f;
            var lapTimeSeconds = floats[1];
            var lastLapTimeSeconds = floats[61];
            var currentLap = (int)Math.Round(floats[36]);
            var distance = floats[2];

            return new TelemetryRecord
            {
                Timestamp = DateTime.UtcNow,
                SpeedKmh = speedKmh,
                Gas = floats[29],
                Brake = floats[31],
                Clutch = floats[32],
                SteerAngle = floats[30],
                CurrentGear = MapGear(gearRaw),
                EngineRpm = engineRpm,
                MaxRpm = maxRpm,
                LapTime = TimeSpan.FromSeconds(lapTimeSeconds),
                LastLapTime = TimeSpan.FromSeconds(lastLapTimeSeconds),
                CurrentLap = currentLap,
                Distance = distance,
                TireTemperatures = new TireValues(floats[57], floats[58], floats[55], floats[56]),
                SessionType = SessionType.Race,
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "DirtRallyTelemetryClient: Error reading telemetry");
            return null;
        }
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
