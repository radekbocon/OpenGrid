using System;
using System.Collections.Generic;
using System.Linq;
using SimLab.Converters;

// ReSharper disable UnusedMember.Global
// ReSharper disable NotAccessedPositionalProperty.Global

namespace SimLab.Models;



public class Session
{
    public SessionInfo Info { get; }
    public List<TelemetryRecord> Records { get; } = [];

    public List<Lap> Laps => Records.GroupBy(x => x.CurrentLap).Select(x => new Lap(x.Key, x.ToList())).ToList();
    
    public Session(SteamGame game, TelemetryRecord record)
    {
        Info = new SessionInfo(game, record);
        Records.Add(record);
    }

    public Session(SessionInfo info, List<TelemetryRecord> records)
    {
        Info = info;
        Records = records;
    }

    public void AddRecord(TelemetryRecord record)
    {
        Records.Add(record);
    }
}

public class SessionInfo
{
    public SessionInfo(SteamGame game, TelemetryRecord record)
    {
        Id = Guid.NewGuid();
        Game = game;
        Type = record.SessionType;
        Track = record.Track;
        Car = record.Car;
        StartTime = record.Timestamp;
    }

    public SessionInfo(Guid id, SteamGame game, SessionType type, string? car, string? track, DateTime startTime)
    {
        Id = id;
        Game = game;
        Type = type;
        Car = car;
        Track = track;
        StartTime = startTime;
    }

    public Guid Id { get; init; }
    public SteamGame Game { get; init; }
    public SessionType Type { get; }
    public string? Car { get; }
    public string? Track { get;  }
    public DateTime StartTime { get; set; }

    public string FileName => $"{Car}-{Track}-{Type}-{Id}.csv";
}

public class Lap
{
    public Lap(int number, List<TelemetryRecord> records)
    {
        Number = number;
        Records = records;
    }

    public int Number { get; }
    public TimeSpan Time => Records.MaxBy(x => x.LapTime)?.LapTime ?? TimeSpan.Zero;
    public bool IsValid => Records.All(x => x.IsValidLap);
    public string DisplayName => $"Lap {Number} ({LapTimeConverter.Format(Time)})";
    public List<TelemetryRecord> Records { get; }
}
