namespace OpenGrid.Models;

/// <summary>
/// A device that can display LED effects driven by telemetry.
/// </summary>
public interface IRpmLedTelemetryDevice : IDevice
{
    bool DriveLedsWithTelemetry { get; set; }
    int RpmLedCount { get; set; }

    string? DefaultLedPresetId { get; set; }
    List<LedPresetOverride> LedOverrides { get; set; }

    bool IsRpmLedActive();
}
