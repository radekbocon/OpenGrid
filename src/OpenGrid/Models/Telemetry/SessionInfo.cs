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
        FileName = CreateFileName();
    }

    public SessionInfo(Guid id, SteamGame game, SessionType type, Car car, Track? track, DateTime startTime, string fileName)
    {
        Id = id;
        Game = game;
        Type = type;
        Car = car;
        Track = track;
        StartTime = startTime;
        FileName = fileName;
    }

    public Guid Id { get; }
    public SteamGame Game { get; }
    public SessionType Type { get; }
    public Car? Car { get; }
    public Track? Track { get;  }
    public DateTime StartTime { get; set; }
    public List<LapInfo> LapInfo { get; set; } = [];
    public string FileName { get; } = string.Empty;

    public string CreateFileName() => $"{Car}-{Track}-{Type}-{Id}.csv";
    
}