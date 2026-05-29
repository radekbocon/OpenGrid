using System;

namespace SimLab.Models.Telemetry;

public record LapInfo(int Number, TimeSpan Time, bool IsValid);