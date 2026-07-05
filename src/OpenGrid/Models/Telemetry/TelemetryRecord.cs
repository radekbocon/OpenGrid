using System.Numerics;

namespace OpenGrid.Models.Telemetry;

public record TelemetryRecord
{
    public DateTime Timestamp { get; init; }
    public Car Car { get; init; }
    public Track Track { get; init; }
    public SessionType SessionType { get; init; }
    public int CurrentLap { get; init; }
    public float SpeedKmh { get; init; }
    public float Gas { get; init; }
    public float Brake { get; init; }
    public float Clutch { get; init; }
    public float SteerAngle { get; init; }
    public float Fuel { get; init; }
    public Gear CurrentGear { get; init; }
    public float EngineRpm { get; init; }
    public TireStats TireTemperatures { get; init; }
    public TimeSpan LapTime { get; init; }
    public float Distance { get; init; }
    public float MaxRpm { get; init; }
    public TireStats TirePressures { get; init; }
    public TimeSpan LastLapTime { get; init; }
    public TimeSpan BestLapTime { get; init; }
    public int AbsSetting { get; init; }
    public int Tc1Setting { get; init; }
    public int Tc2Setting { get; init; }
    public TimeSpan DeltaLapTime { get; init; }
    public int Position { get; init; }
    public int EngineMap { get; init; }
    public float BrakeBias { get; init; }
    public bool IsDeltaPositive { get; init; }
    public bool IsValidLap { get; init; }
    public int SectorIndex { get; init; }
    public TimeSpan LastSectorTime { get; init; }
    public Vector3 CarPosition { get; init; }
    public float GForceLat { get; init; }
    public float GForceLon { get; init; }
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

public record struct TireStats(float FrontLeft, float FrontRight, float RearLeft, float RearRight)
{
    public static TireStats FromArray(float[] array)
    {
        return array.Length != 4 
            ? new TireStats(0, 0, 0, 0) 
            : new TireStats(array[0], array[1], array[2], array[3]);
    }
}

public static class GearExtensions
{
    public static string DisplayName(this Gear gear)
    {
        return gear switch
        {
            Gear.R => "R",
            Gear.N => "N",
            Gear.N1 => "1",
            Gear.N2 => "2",
            Gear.N3 => "3",
            Gear.N4 => "4",
            Gear.N5 => "5",
            Gear.N6 => "6",
            _ => ""
        };
    }
}