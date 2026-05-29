using System.Collections.Generic;
using System.Linq;

namespace SimLab.Models.Telemetry;

public class SessionDetails
{
    public Session Session { get; }
    public SessionInfo Info => Session.Info;
    public List<TelemetryRecord> Records { get; }

    public List<Lap> Laps
    {
        get
        {
            if (field is not null)
            {
                return field;
            }

            field = Records
                .GroupBy(x => x.CurrentLap)
                .Select(x => new Lap(x.Key, x.ToList()))
                .OrderBy(x => x.Number)
                .ToList();

            field.MinBy(x => x.Time)?.IsFastest = true;
            return field;
        }
    }

    public SessionDetails(Session session, List<TelemetryRecord> records)
    {
        Session = session;
        Records = records;
    }

    public SessionDetails(SteamGame game, TelemetryRecord record)
    {
        var info = new SessionInfo(game, record);
        Session = new Session(info);
        Records = [record];
    }

    public void AddRecord(TelemetryRecord record)
    {
        Records.Add(record);
    }
}