using System;
using System.Numerics;
using OpenGrid.Models.Telemetry;

namespace OpenGrid.Services.SessionPersist;

public class TelemetryCsvRow
{
    public double Timestamp { get; set; }
    public int CurrentLap { get; set; }
    public double SpeedKmh { get; set; }
    public double Gas { get; set; }
    public double Brake { get; set; }
    public double Clutch { get; set; }
    public double SteerAngle { get; set; }
    public double Fuel { get; set; }
    public int CurrentGear { get; set; }
    public double EngineRpm { get; set; }
    public double TireTemperatureFL { get; set; }
    public double TireTemperatureFR { get; set; }
    public double TireTemperatureRL { get; set; }
    public double TireTemperatureRR { get; set; }
    public double LapTime { get; set; }
    public double Distance { get; set; }
    public double MaxRpm { get; set; }
    public double TirePressureFL { get; set; }
    public double TirePressureFR { get; set; }
    public double TirePressureRL { get; set; }
    public double TirePressureRR { get; set; }
    public double LastLapTime { get; set; }
    public double BestLapTime { get; set; }
    public int AbsSetting { get; set; }
    public int Tc1Setting { get; set; }
    public int Tc2Setting { get; set; }
    public double DeltaLapTime { get; set; }
    public int Position { get; set; }
    public int EngineMap { get; set; }
    public double BrakeBias { get; set; }
    public bool IsDeltaPositive { get; set; }
    public bool IsValidLap { get; set; }
    public int SectorIndex { get; set; }
    public double LastSectorTimeSec { get; set; }
    public double PosX { get; set; }
    public double PosY { get; set; }
    public double PosZ { get; set; }
    public double GForceLat { get; set; }
    public double GForceLon { get; set; }

    public static TelemetryCsvRow FromRecord(TelemetryRecord r)
    {
        return new TelemetryCsvRow
        {
            Timestamp = SessionWriter.ToUnixSeconds(r.Timestamp),
            CurrentLap = r.CurrentLap,
            SpeedKmh = r.SpeedKmh,
            Gas = r.Gas,
            Brake = r.Brake,
            Clutch = r.Clutch,
            SteerAngle = r.SteerAngle,
            Fuel = r.Fuel,
            CurrentGear = (int)r.CurrentGear,
            EngineRpm = r.EngineRpm,
            TireTemperatureFL = r.TireTemperatures.FrontLeft,
            TireTemperatureFR = r.TireTemperatures.FrontRight,
            TireTemperatureRL = r.TireTemperatures.RearLeft,
            TireTemperatureRR = r.TireTemperatures.RearRight,
            LapTime = r.LapTime.TotalSeconds,
            Distance = r.Distance,
            MaxRpm = r.MaxRpm,
            TirePressureFL = r.TirePressures.FrontLeft,
            TirePressureFR = r.TirePressures.FrontRight,
            TirePressureRL = r.TirePressures.RearLeft,
            TirePressureRR = r.TirePressures.RearRight,
            LastLapTime = r.LastLapTime.TotalSeconds,
            BestLapTime = r.BestLapTime.TotalSeconds,
            AbsSetting = r.AbsSetting,
            Tc1Setting = r.Tc1Setting,
            Tc2Setting = r.Tc2Setting,
            DeltaLapTime = r.DeltaLapTime.TotalSeconds,
            Position = r.Position,
            EngineMap = r.EngineMap,
            BrakeBias = r.BrakeBias,
            IsDeltaPositive = r.IsDeltaPositive,
            IsValidLap = r.IsValidLap,
            SectorIndex = r.SectorIndex,
            LastSectorTimeSec = r.LastSectorTime.TotalSeconds,
            PosX = r.CarPosition.X,
            PosY = r.CarPosition.Y,
            PosZ = r.CarPosition.Z,
            GForceLat = r.GForceLat,
            GForceLon = r.GForceLon,
        };
    }

    public TelemetryRecord ToRecord()
    {
        return new TelemetryRecord
        {
            Timestamp = SessionWriter.FromUnixSeconds(Timestamp),
            CurrentLap = CurrentLap,
            SpeedKmh = (float)SpeedKmh,
            Gas = (float)Gas,
            Brake = (float)Brake,
            Clutch = (float)Clutch,
            SteerAngle = (float)SteerAngle,
            Fuel = (float)Fuel,
            CurrentGear = (Gear)CurrentGear,
            EngineRpm = (float)EngineRpm,
            TireTemperatures = new TireStats(
                (float)TireTemperatureFL, (float)TireTemperatureFR,
                (float)TireTemperatureRL, (float)TireTemperatureRR),
            LapTime = TimeSpan.FromSeconds(LapTime),
            Distance = (float)Distance,
            MaxRpm = (float)MaxRpm,
            TirePressures = new TireStats(
                (float)TirePressureFL, (float)TirePressureFR,
                (float)TirePressureRL, (float)TirePressureRR),
            LastLapTime = TimeSpan.FromSeconds(LastLapTime),
            BestLapTime = TimeSpan.FromSeconds(BestLapTime),
            AbsSetting = AbsSetting,
            Tc1Setting = Tc1Setting,
            Tc2Setting = Tc2Setting,
            DeltaLapTime = TimeSpan.FromSeconds(DeltaLapTime),
            Position = Position,
            EngineMap = EngineMap,
            BrakeBias = (float)BrakeBias,
            IsDeltaPositive = IsDeltaPositive,
            IsValidLap = IsValidLap,
            SectorIndex = SectorIndex,
            LastSectorTime = TimeSpan.FromSeconds(LastSectorTimeSec),
            CarPosition = new Vector3((float)PosX, (float)PosY, (float)PosZ),
            GForceLat = (float)GForceLat,
            GForceLon = (float)GForceLon,
        };
    }
}
