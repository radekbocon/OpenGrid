using System;
using System.Collections.Generic;
using System.Linq;
using SimLab.Converters;

// ReSharper disable UnusedMember.Global
// ReSharper disable NotAccessedPositionalProperty.Global

namespace SimLab.Models.Telemetry;

public class Lap
{
    private readonly TimeSpan? _headerTime;
    private readonly bool? _headerIsValid;

    public Lap(int number, List<TelemetryRecord> records)
    {
        Number = number;
        Records = records;
    }

    public Lap(int number, TimeSpan time, bool isValid)
    {
        Number = number;
        _headerTime = time;
        _headerIsValid = isValid;
        Records = null;
    }

    public int Number { get; }
    public TimeSpan Time => _headerTime ?? Records?.MaxBy(x => x.LapTime)?.LapTime ?? TimeSpan.Zero;
    public bool IsValid => _headerIsValid ?? Records?.All(x => x.IsValidLap) ?? false;
    public string DisplayName => $"Lap {Number} ({LapTimeConverter.Format(Time)})";
    public List<TelemetryRecord>? Records { get; }
    public bool IsFastest { get; set; }
}
