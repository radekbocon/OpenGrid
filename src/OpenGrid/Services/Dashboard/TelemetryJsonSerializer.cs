using System.Numerics;
using System.Text.Json;
using OpenGrid.Models.Telemetry;

namespace OpenGrid.Services.Dashboard;

public static class TelemetryJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string Serialize(TelemetryRecord record)
    {
        return JsonSerializer.Serialize(new
        {
            timestamp = record.Timestamp,
            car = new
            {
                key = record.Car.Key,
                displayName = record.Car.DisplayName
            },
            track = new
            {
                key = record.Track.Key,
                displayName = record.Track.DisplayName
            },
            sessionType = record.SessionType.ToString(),
            currentLap = record.CurrentLap,
            speedKmh = record.SpeedKmh,
            gas = record.Gas,
            brake = record.Brake,
            clutch = record.Clutch,
            steerAngle = record.SteerAngle,
            fuel = record.Fuel,
            gear = record.CurrentGear.DisplayName(),
            engineRpm = record.EngineRpm,
            maxRpm = record.MaxRpm,
            tireTemps = new
            {
                fl = record.TireTemperatures.FrontLeft,
                fr = record.TireTemperatures.FrontRight,
                rl = record.TireTemperatures.RearLeft,
                rr = record.TireTemperatures.RearRight
            },
            tirePressures = new
            {
                fl = record.TirePressures.FrontLeft,
                fr = record.TirePressures.FrontRight,
                rl = record.TirePressures.RearLeft,
                rr = record.TirePressures.RearRight
            },
            lapTimeMs = record.LapTime.TotalMilliseconds,
            lastLapTimeMs = record.LastLapTime.TotalMilliseconds,
            bestLapTimeMs = record.BestLapTime.TotalMilliseconds,
            deltaLapTimeMs = record.DeltaLapTime.TotalMilliseconds,
            isDeltaPositive = record.IsDeltaPositive,
            isValidLap = record.IsValidLap,
            distance = record.Distance,
            sectorIndex = record.SectorIndex,
            lastSectorTimeMs = record.LastSectorTime.TotalMilliseconds,
            position = record.Position,
            absSetting = record.AbsSetting,
            tc1Setting = record.Tc1Setting,
            tc2Setting = record.Tc2Setting,
            engineMap = record.EngineMap,
            brakeBias = record.BrakeBias,
            carPosition = new
            {
                x = record.CarPosition.X,
                y = record.CarPosition.Y,
                z = record.CarPosition.Z
            },
            gForceLat = record.GForceLat,
            gForceLon = record.GForceLon
        }, Options);
    }
}
