using System;
using OpenGrid.Models;
using OpenGrid.Models.Telemetry;

namespace OpenGrid.Services.SessionPersist;

public class SessionMetaRow
{
    public string Id { get; set; } = string.Empty;
    public int GameAppId { get; set; }
    public string CarKey { get; set; } = string.Empty;
    public string TrackKey { get; set; } = string.Empty;
    public int SessionType { get; set; }
    public double StartTime { get; set; }

    public static SessionMetaRow FromSessionInfo(SessionInfo info)
    {
        return new SessionMetaRow
        {
            Id = info.Id.ToString(),
            GameAppId = info.Game.AppId,
            CarKey = info.Car?.Key ?? "",
            TrackKey = info.Track?.Key ?? "",
            SessionType = (int)info.Type,
            StartTime = SessionWriter.ToUnixSeconds(info.StartTime),
        };
    }

    public static SessionInfo? ToSessionInfo(SessionMetaRow row)
    {
        var game = SteamGame.GetByAppId(row.GameAppId);
        if (game is null)
            return null;

        return new SessionInfo(
            Guid.Parse(row.Id),
            game,
            (SessionType)row.SessionType,
            Car.Create(row.CarKey),
            Track.Create(row.TrackKey),
            SessionWriter.FromUnixSeconds(row.StartTime));
    }
}
