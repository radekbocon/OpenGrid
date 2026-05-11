using System;
using System.Threading;
using System.Threading.Tasks;
using SimLab.Models;

namespace SimLab.Services.SharedMemory;

public class DebugTelemetryClient : ITelemetryClient
{
    private bool _connected;
    private int _tick;
    private readonly Random _random = new(1337);

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Simulate some startup delay
            await Task.Delay(100, cancellationToken);
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
        var steer = (float)Math.Sin(_tick * 0.15) * 20f; // -20..20 deg
        var fuel = Math.Max(0f, 100f - _tick * 0.25f);

        var currentLap = (_tick / 60) + 1;
        var currentGear = (Gear)(_tick % 8);
        var engineRpm = 5000f + (float)(Math.Abs(Math.Sin(_tick * 0.2)) * 7000f);
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

        var snapshot = new TelemetryRecord
        {
            RecordedAt = now,
            Car = "DebugCar",
            Track = "DebugTrack",
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
            TireTemperatures = TireValues.FromArray(temps),
            LapTime = TimeSpan.FromSeconds(75 + (_tick % 30)),
            Distance = (float)(_tick * 1.5),
            MaxRpm = 12000f,
            TirePressures = TireValues.FromArray(pressures)
        };

        return snapshot;
    }
}
