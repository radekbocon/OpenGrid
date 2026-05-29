using System.Collections.Generic;
using System.Linq;

namespace SimLab.Models.Telemetry;

public class Session
{
    public SessionInfo Info { get; }

    public List<Lap> Laps
    {
        get
        {
            if (field is not null)
            {
                return field;
            }

            if (Info.LapHeaders.Count > 0)
            {
                field = Info.LapHeaders
                    .Select(h => new Lap(h.Number, h.Time, h.IsValid))
                    .ToList();
            }

            field?.MinBy(x => x.Time)?.IsFastest = true;
            return field ?? [];
        }
    }

    public Session(SessionInfo info)
    {
        Info = info;
    }
}