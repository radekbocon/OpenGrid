using System;
using System.Collections.Generic;
using System.Linq;
using OpenGrid.Converters;

// ReSharper disable UnusedMember.Global
// ReSharper disable NotAccessedPositionalProperty.Global

namespace OpenGrid.Models.Telemetry;

public class Lap
{
    private readonly TimeSpan? _headerTime;
    private readonly bool? _headerIsValid;
    private readonly TimeSpan _sector1;
    private readonly TimeSpan _sector2;
    private readonly TimeSpan _sector3;

    public Lap(int number, List<TelemetryRecord> records)
    {
        Number = number;
        Records = records;
        ComputeSectors(records, out _sector1, out _sector2, out _sector3);
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
    public TimeSpan Sector1Time => _headerTime.HasValue ? TimeSpan.Zero : _sector1;
    public TimeSpan Sector2Time => _headerTime.HasValue ? TimeSpan.Zero : _sector2;
    public TimeSpan Sector3Time => _headerTime.HasValue ? TimeSpan.Zero : _sector3;

    public double AvgSpeedKmh => Records is { Count: > 0 } ? Math.Round(Records.Average(r => r.SpeedKmh), 1) : 0;
    public double MaxSpeedKmh => Records is { Count: > 0 } ? Math.Round(Records.Max(r => r.SpeedKmh), 1) : 0;
    public double MinSpeedKmh => Records is { Count: > 0 } ? Math.Round(Records.Min(r => r.SpeedKmh), 1) : 0;
    public double FlatOutPercent => Records is { Count: > 0 } ? Math.Round((double)Records.Count(r => r.Gas > 0.95f) / Records.Count * 100, 1) : 0;
    public double AvgGas => Records is { Count: > 0 } ? Math.Round(Records.Average(r => r.Gas), 3) : 0;
    public double AvgBrake => Records is { Count: > 0 } ? Math.Round(Records.Average(r => r.Brake), 3) : 0;
    public double MaxGForceLat => Records is { Count: > 0 } ? Math.Round(Records.Max(r => r.GForceLat), 2) : 0;
    public double MaxGForceLon => Records is { Count: > 0 } ? Math.Round(Records.Max(r => r.GForceLon), 2) : 0;
    public double MinGForceLon => Records is { Count: > 0 } ? Math.Round(Records.Min(r => r.GForceLon), 2) : 0;
    public double DistanceKm => Records is { Count: > 0 } ? Math.Round((Records.Max(r => r.Distance) - Records.Min(r => r.Distance)) / 1000.0, 2) : 0;

    private static void ComputeSectors(List<TelemetryRecord> records, out TimeSpan s1, out TimeSpan s2, out TimeSpan s3)
    {
        s1 = s2 = s3 = TimeSpan.Zero;

        var lapTime = records.MaxBy(x => x.LapTime)?.LapTime;
        if (lapTime is null || lapTime.Value <= TimeSpan.Zero)
            return;

        var splits = records
            .Select(r => r.LastSectorTime)
            .Where(t => t > TimeSpan.Zero && t < lapTime.Value)
            .Distinct()
            .OrderBy(t => t)
            .Take(2)
            .ToList();

        if (splits.Count >= 1)
        {
            s1 = splits[0];
        }

        if (splits.Count >= 2)
        {
            s2 = splits[1] - splits[0];
        }

        s3 = lapTime.Value - s1 - s2;
    }
}
