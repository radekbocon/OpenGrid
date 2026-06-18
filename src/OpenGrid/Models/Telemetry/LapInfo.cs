using System;

namespace OpenGrid.Models.Telemetry;

public record LapInfo(int Number, TimeSpan Time, bool IsValid)
{
    public bool IsFastest { get; set; }
}