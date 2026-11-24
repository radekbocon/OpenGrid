namespace OpenGrid.Models;

/// <summary>
/// A device that can display RPM LED effects driven by telemetry.
/// </summary>
public interface IRpmLedTelemetryDevice : IDevice
{
    bool DriveLedsWithTelemetry { get; set; }
    int RpmLedCount { get; set; }
    List<RpmLedSegment> RpmSegments { get; set; }
    bool IsRpmLedActive();
    List<RpmLedSegment> GetNormalizedRpmSegments();
}
