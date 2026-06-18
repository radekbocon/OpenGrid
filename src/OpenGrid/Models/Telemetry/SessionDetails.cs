using System.Collections.Generic;
using System.Linq;

namespace OpenGrid.Models.Telemetry;

public class SessionDetails
{
    private List<Lap>? _laps;
    public SessionInfo Info { get; }
    public List<TelemetryRecord> Records { get; }

    public List<Lap> Laps
    {
        get
        {
            if (_laps is not null)
            {
                return _laps;
            }

            _laps = Records
                .GroupBy(x => x.CurrentLap)
                .Select(x => new Lap(x.Key, x.ToList()))
                .OrderBy(x => x.Number)
                .ToList();

            _laps.MinBy(x => x.Time)?.IsFastest = true;
            return _laps;
        }
    }

    public SessionDetails(SessionInfo session, List<TelemetryRecord> records)
    {
        Info = session;
        Records = records;
    }

    public SessionDetails(SteamGame game, TelemetryRecord record)
    {
        Info = new SessionInfo(game, record);
        Records = [record];
    }

    public void AddRecord(TelemetryRecord record)
    {
        Records.Add(record);
    }
    
    public void DeleteLap(int lapNumber)
    {
        Records.RemoveAll(r => r.CurrentLap == lapNumber);
        Info.LapInfo.RemoveAll(l => l.Number == lapNumber);
        _laps = null;
    }
}