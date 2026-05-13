using System;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;

namespace SimLab.Models;

[ProtoContract]
public record TelemetryRecord
{
    [ProtoMember(1)]
    public DateTime RecordedAt { get; set; }
    [ProtoMember(2)]
    public string? Car { get; set; }
    [ProtoMember(3)]
    public string? Track { get; set; }
    [ProtoMember(4)]
    public SessionType SessionType { get; set; }
    [ProtoMember(5)]
    public int CurrentLap { get; set; }
    [ProtoMember(6)]
    public float SpeedKmh { get; set; }
    [ProtoMember(7)]
    public float Gas { get; set; }
    [ProtoMember(8)]
    public float Brake { get; set; }
    [ProtoMember(9)]
    public float Clutch { get; set; }
    [ProtoMember(10)]
    public float SteerAngle { get; set; }
    [ProtoMember(11)]
    public float Fuel { get; set; }
    [ProtoMember(12)]
    public Gear CurrentGear { get; set; }
    [ProtoMember(13)]
    public float EngineRpm { get; set; }
    [ProtoMember(14)]
    public TireValues TireTemperatures { get; set; }
    [ProtoMember(15)]
    public TimeSpan LapTime { get; set; }
    [ProtoMember(16)]
    public float Distance { get; set; }
    [ProtoMember(17)]
    public float MaxRpm { get; set; }
    [ProtoMember(18)]
    public TireValues TirePressures { get; set; }
    [ProtoMember(19)]
    public TimeSpan LastLapTime { get; set; }
    [ProtoMember(20)]
    public TimeSpan BestLapTime { get; set; }
    [ProtoMember(21)]
    public int AbsSetting { get; set; }
    [ProtoMember(22)]
    public int Tc1Setting { get; set; }
    [ProtoMember(23)]
    public int Tc2Setting { get; set; }
    [ProtoMember(24)]
    public TimeSpan DeltaLapTime { get; set; }
    [ProtoMember(25)]
    public int Position { get; set; }
    [ProtoMember(26)]
    public int EngineMap { get; set; }
    [ProtoMember(27)]
    public float BrakeBias { get; set; }
    [ProtoMember(28)]
    public bool IsDeltaPositive { get; set; }
    [ProtoMember(29)]
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

[ProtoContract]
public record struct TireValues(float FrontLeft, float FrontRight, float RearLeft, float RearRight)
{
    public static TireValues FromArray(float[] array)
    {
        return array.Length != 4 
            ? new TireValues(0, 0, 0, 0) 
            : new TireValues(array[0], array[1], array[2], array[3]);
    }
}

[ProtoContract]
public record Session
{
    [ProtoMember(1)]
    public SessionInfo Info { get; }
    [ProtoMember(2)]
    public List<Lap> Laps { get; } = [];
    public Lap CurrentLap => Laps.Last();

    private Session()
    {
        
    }

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
        return record.CurrentLap != CurrentLap.Number || record.LapTime < CurrentLap.Time;
    }
}

[ProtoContract]
public record SessionInfo
{
    public SessionInfo()
    {
        
    }
    public SessionInfo(SteamGame game, TelemetryRecord record)
    {
        Id = Guid.NewGuid();
        Game = game;
        Type = record.SessionType;
        Track = record.Track;
        Car = record.Car;
        StartTime = record.RecordedAt;
    }

    [ProtoMember(1)]
    public Guid Id { get; init; }
    [ProtoMember(2)]
    public SteamGame Game { get; init; }
    [ProtoMember(3)]
    public SessionType Type { get; }
    [ProtoMember(4)]
    public string? Car { get; }
    [ProtoMember(5)]
    public string? Track { get;  }
    [ProtoMember(6)]
    public DateTime StartTime { get; set; }
    [ProtoMember(7)]
    public DateTime EndTime { get; set; }
}

[ProtoContract]
public record Lap
{
    public Lap()
    {
        
    }
    
    public Lap(int number, TelemetryRecord record)
    {
        Number = number;
        Records.Add(record);
    }

    [ProtoMember(1)]
    public int Number { get; }
    public TimeSpan Time => Records.MaxBy(x => x.LapTime)?.LapTime ?? TimeSpan.Zero;
    [ProtoMember(2)]
    public List<TelemetryRecord> Records { get; set; } = [];
}