using System;

namespace SimLab.Models;

public record TelemetryRecord
{
    public DateTime Timestamp { get; set; }
    public string? Car { get; set; }
    public string? Track { get; set; }
    public SessionType SessionType { get; set; }
    public int CurrentLap { get; set; }
    public float SpeedKmh { get; set; }
    public float Gas { get; set; }
    public float Brake { get; set; }
    public float Clutch { get; set; }
    public float SteerAngle { get; set; }
    public float Fuel { get; set; }
    public Gear CurrentGear { get; set; }
    public float EngineRpm { get; set; }
    public TireValues TireTemperatures { get; set; }
    public TimeSpan LapTime { get; set; }
    public float Distance { get; set; }
    public float MaxRpm { get; set; }
    public TireValues TirePressures { get; set; }
    public TimeSpan LastLapTime { get; set; }
    public TimeSpan BestLapTime { get; set; }
    public int AbsSetting { get; set; }
    public int Tc1Setting { get; set; }
    public int Tc2Setting { get; set; }
    public TimeSpan DeltaLapTime { get; set; }
    public int Position { get; set; }
    public int EngineMap { get; set; }
    public float BrakeBias { get; set; }
    public bool IsDeltaPositive { get; set; }
    public bool IsValidLap { get; set; }
}

public enum SessionType
{
    Unknown = -1,
    Practice = 0,
    Qualify = 1,
    Race = 2,
    HotLap = 3,
    TimeAttack = 4,
    Drift = 5,
    Drag = 6,
    FormationLap = 7
}

public enum Gear
{
    R = 0,
    N = 1,
    N1 = 2,
    N2 = 3,
    N3 = 4,
    N4 = 5,
    N5 = 6,
    N6 = 7
}

public record struct TireValues(float FrontLeft, float FrontRight, float RearLeft, float RearRight)
{
    public static TireValues FromArray(float[] array)
    {
        return array.Length != 4 
            ? new TireValues(0, 0, 0, 0) 
            : new TireValues(array[0], array[1], array[2], array[3]);
    }
}