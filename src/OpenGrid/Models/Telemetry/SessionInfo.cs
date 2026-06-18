using System;
using System.Collections.Generic;

namespace OpenGrid.Models.Telemetry;

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

    public SessionInfo(Guid id, SteamGame game, SessionType type, Car car, Track? track, DateTime startTime)
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
    public Car? Car { get; }
    public Track? Track { get;  }
    public DateTime StartTime { get; set; }
    public List<LapInfo> LapInfo { get; set; } = [];

    public string FileName => $"{Car}-{Track}-{Type}-{Id}.csv";
}