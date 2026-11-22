namespace OpenGrid.Models;

/// <summary>
/// A device connected through a serial port. Currently used for MOZA Racing
/// wheelbases and other MOZA devices that expose the internal bus over USB CDC.
/// </summary>
public class SerialDevice : IDevice
{
    public required string Id { get; init; }
    public DeviceType DeviceType { get; init; } = DeviceType.Serial;
    public required string Name { get; init; }
    public string? Description { get; set; }
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Stable path to the port (/dev/serial/by-id/... when available).
    /// </summary>
    public required string PortName { get; init; }

    public string? SerialNumber { get; init; }
    public ushort VendorId { get; init; }
    public ushort ProductId { get; init; }
    public MozaDeviceCategory Category { get; init; }

    public DeviceConnectionStatus Status { get; set; }

    /// <summary>
    /// Drive the wheel RPM LEDs from game telemetry (requires the global
    /// setting to be enabled as well).
    /// </summary>
    public bool DriveLedsWithTelemetry { get; set; } = true;

    /// <summary>
    /// Number of LEDs in the wheel's RPM strip. Colour and idle behaviour are
    /// configured on the wheel itself (Pit House / boxflat).
    /// </summary>
    public int RpmLedCount { get; set; } = 10;
}

public enum MozaDeviceCategory
{
    Unknown,
    WheelBase,
    Wheel,
    Pedals,
    Dashboard,
    Shifter,
    Handbrake,
    Hub,
    Other,
}
