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

    public List<Lap> Laps
    {
        get
        {
            if (field is not null)
            {
                return field;
            }

            var laps = Records
                .GroupBy(x => x.CurrentLap)
                .Select(x => new Lap(x.Key, x.ToList()))
                .OrderBy(x => x.Number)
                .ToList();
            
            laps.MinBy(x => x.Time)?.IsFastest = true;
            
            field = laps;
            return field;
        }
    }

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

    public SessionInfo(Guid id, SteamGame game, SessionType type, Car car, Track track, DateTime startTime)
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
    public Car Car { get; }
    public Track? Track { get;  }
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
    public bool IsFastest { get; set; }
}
