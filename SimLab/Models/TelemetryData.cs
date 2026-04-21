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

/// <summary>
/// Information about a single lap
/// </summary>
public class LapInfo
{
    public int LapNumber { get; set; }
    public TimeSpan? LapTime { get; set; }
    public float FuelUsed { get; set; }
    public float AverageTireTemp { get; set; }
    public bool IsValid { get; set; }
    public DateTime RecordedAt { get; set; }
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
    public int CurrentGear { get; set; }
    public float EngineRpm { get; set; }
    public float FuelRemaining { get; set; }
    public float[] TireTemperatures { get; set; } = new float[4]; // FL, FR, RL, RR
    public bool IsOnTrack { get; set; }
    public bool IsInPit { get; set; }
}

/// <summary>
/// Session data aggregating multiple snapshots and laps
/// </summary>
public class SessionData
{
    public DateTime SessionStartTime { get; set; }
    public string? Track { get; set; }
    public SessionType SessionType { get; set; }
    public string? CarModel { get; set; }
    public List<LapInfo> Laps { get; set; } = new();
    public List<TelemetrySnapshot> Snapshots { get; set; } = new();

    public LapInfo? BestLap => Laps.Where(l => l.IsValid && l.LapTime.HasValue).MinBy(l => l.LapTime);
    public double AverageLapTime => Laps.Where(l => l.IsValid && l.LapTime.HasValue).Average(l => l.LapTime.Value.TotalMilliseconds);
    public int ValidLaps => Laps.Count(l => l.IsValid);
    public TimeSpan? TotalSessionTime => Snapshots.Count > 0 ? Snapshots.Last().RecordedAt - Snapshots.First().RecordedAt : null;
}

