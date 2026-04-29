using System;
using System.Collections.Generic;
using System.Linq;

namespace SimLab.Models;

public record TelemetryRecord
{
    public DateTime RecordedAt { get; set; }
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

public record Session
{
    public SessionInfo Info { get; }
    public List<Lap> Laps { get; } = [];
    public Lap CurrentLap => Laps.Last();

    public Session(SteamGame game, TelemetryRecord record)
    {
        Info = new SessionInfo(game, record);
        Laps.Add(new Lap(record.CurrentLap, record));
    }

    public bool IsNewSession(TelemetryRecord record)
    {
        return record.SessionType != Info.Type || record.Track != Info.Track || record.Car != Info.Car;
    }

    public bool IsNewLap(TelemetryRecord record)
    {
        return record.CurrentLap != CurrentLap.Number;
    }
}

public record SessionInfo
{
    public SessionInfo(SteamGame game, TelemetryRecord record)
    {
        Id = Guid.NewGuid();
        Game = game;
        Type = record.SessionType;
        Track = record.Track;
        Car = record.Car;
        StartTime = record.RecordedAt;
    }

    public Guid Id { get; init; }
    public SteamGame Game { get; init; }
    public SessionType Type { get; }
    public string? Car { get; }
    public string? Track { get;  }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}

public record Lap
{
    public Lap(int number, TelemetryRecord record)
    {
        Number = number;
        Records.Add(record);
    }

    public int Number { get; }
    public TimeSpan Time => Records.MaxBy(x => x.LapTime)?.LapTime ?? TimeSpan.Zero;
    public List<TelemetryRecord> Records { get; set; } = [];
}