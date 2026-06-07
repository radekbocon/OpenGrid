using System;
using SimLab.Models.Telemetry;

namespace SimLab.Services.SessionPersist;

public class LapInfoRow
{
    public int Number { get; set; }
    public double Time { get; set; }
    public bool IsValid { get; set; }

    public static LapInfoRow FromLap(Lap lap)
    {
        return new LapInfoRow
        {
            Number = lap.Number,
            Time = lap.Time.TotalSeconds,
            IsValid = lap.IsValid,
        };
    }

    public static LapInfoRow FromLapInfo(LapInfo lap)
    {
        return new LapInfoRow
        {
            Number = lap.Number,
            Time = lap.Time.TotalSeconds,
            IsValid = lap.IsValid,
        };
    }

    public LapInfo ToLapInfo()
    {
        return new LapInfo(Number, TimeSpan.FromSeconds(Time), IsValid);
    }
}
