using OpenGrid.Models.Telemetry;
using Vector3 = System.Numerics.Vector3;

namespace OpenGrid.Services.Telemetry;

public class DebugTelemetryClient : ITelemetryClient
{
    private bool _connected;
    private int _tick;
    private readonly Random _random = new(1337);

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            _connected = true;
            _tick = 0;
            return true;
        }
        catch
        {
            _connected = false;
            return false;
        }
    }

    public void Stop()
    {
        _connected = false;
    }

    public TelemetryRecord? ReadTelemetry()
    {
        if (!_connected)
        {
            return null;
        }

        _tick++;
        // Generate a small, predictable, fake telemetry snapshot
        var now = DateTime.UtcNow;
        var speed = 60f + (float)Math.Sin(_tick * 0.3) * 40f; // 20-100 range
        var gas = 0.4f + (float)((Math.Sin(_tick * 0.2) + 1) * 0.25); // 0.4 - 0.95
        var brake = Math.Max(0f, (float)Math.Sin(_tick * 0.25) * 0.2f);
        var clutch = 0f;
        var steer = (float)Math.Sin(_tick * 0.15); // -20..20 deg
        var fuel = Math.Max(0f, 100f - _tick * 0.25f);

        var currentLap = (_tick / 1000) + 1;
        var currentGear = (Gear)(_tick % 8);
        var engineRpm = 0f + (float)(Math.Abs(Math.Sin(_tick * 0.01)) * 12000f);
        var abs = Math.Clamp((brake - 0.05f) / 0.15f, 0f, 1f);
        var tc = Math.Clamp((gas - 0.6f) / 0.35f, 0f, 1f);
        float[] temps = [
            85f + (float)_random.NextDouble() * 15f,
            87f + (float)_random.NextDouble() * 15f, 
            82f + (float)_random.NextDouble() * 10f, 
            84f + (float)_random.NextDouble() * 10f
        ];
        float[] pressures = [
            85f + (float)_random.NextDouble() * 15f,
            87f + (float)_random.NextDouble() * 15f, 
            82f + (float)_random.NextDouble() * 10f, 
            84f + (float)_random.NextDouble() * 10f
        ];

        var lapTime = TimeSpan.FromSeconds(75 + (_tick % 30));
        var lastLapTime = TimeSpan.FromSeconds(85 + (_tick % 10));
        var bestLapTime = TimeSpan.FromSeconds(82);
        var deltaMs = (float)(Math.Sin(_tick * 0.1) * 3000);
        var deltaLapTime = TimeSpan.FromMilliseconds(deltaMs);

        var snapshot = new TelemetryRecord
        {
            Timestamp = now,
            Car = Car.Create("DebugCar"),
            Track = Track.Create("DebugTrack"),
            SessionType = SessionType.Race,
            CurrentLap = currentLap,
            SpeedKmh = speed,
            Gas = gas,
            Brake = brake,
            Clutch = clutch,
            SteerAngle = steer,
            Fuel = fuel,
            CurrentGear = currentGear,
            EngineRpm = engineRpm,
            TireTemperatures = TireStats.FromArray(temps),
            LapTime = lapTime,
            LastLapTime = lastLapTime,
            BestLapTime = bestLapTime,
            DeltaLapTime = deltaLapTime,
            IsDeltaPositive = deltaMs > 0,
            Position = (_tick / 100) % 20 + 1,
            AbsSetting = (_tick / 50) % 4,
            Tc1Setting = (_tick / 30) % 12,
            Tc2Setting = (_tick / 30) % 12,
            Abs = abs,
            Tc = tc,
            EngineMap = (_tick / 100) % 8 + 1,
            BrakeBias = 58f + (float)Math.Sin(_tick * 0.05) * 10f,
            Distance = (float)(_tick * 1.5),
            MaxRpm = 12000f,
            TirePressures = TireStats.FromArray(pressures),
            IsValidLap = true,
            SectorIndex = currentLap % 3 + 1,
            LastSectorTime = TimeSpan.FromSeconds((_tick % 1000) switch
            {
                < 200 => 0,
                < 500 => 25.5,
                < 800 => 55.3,
                _ => 0
            }),
            GForceLat = (float)(Math.Sin(_tick * 0.1) * 1.5),
            GForceLon = (float)(Math.Cos(_tick * 0.08) * 1.5),
            CarPosition = new Vector3(
                100f + (float)Math.Sin(_tick * 0.05f) * 50f,
                50f + (float)Math.Cos(_tick * 0.03f) * 30f,
                100f + (float)Math.Cos(_tick * 0.05f) * 50f)
        };

        return snapshot;
    }
}
