using System;
using System.Collections.Generic;
using System.Linq;

namespace SimLab.Models;

/// <summary>
/// Session type enumeration
/// </summary>
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

/// <summary>
/// Telemetry snapshot for a specific moment in time
/// </summary>
public class TelemetrySnapshot
{
    public DateTime RecordedAt { get; set; }
    public string? Track { get; set; }
    public SessionType SessionType { get; set; }
    public int CurrentLap { get; set; }
    public float SpeedKmh { get; set; }
    public float Gas { get; set; }
    public float Brake { get; set; }
    public float Clutch { get; set; }
    public Gear CurrentGear { get; set; }
    public float EngineRpm { get; set; }
    public float FuelRemaining { get; set; }
    public float[] TireTemperatures { get; set; } = new float[4]; // FL, FR, RL, RR
    public bool IsInPit { get; set; }
    public bool IsValidLap { get; set; }
    public TimeSpan LapTime { get; set; }
}

