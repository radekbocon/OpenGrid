using System;

namespace SimLab.Models;

public record TelemetryRecord
{
    public DateTime RecordedAt { get; set; }
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
    public TireTemperatures TireTemperatures { get; set; }
    public TimeSpan LapTime { get; set; }
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

public record struct TireTemperatures(float FrontLeft, float FrontRight, float RearLeft, float RearRight)
{
    public static TireTemperatures FromArray(float[] array)
    {
        return array.Length != 4 
            ? new TireTemperatures(0, 0, 0, 0) 
            : new TireTemperatures(array[0], array[1], array[2], array[3]);
    }
}