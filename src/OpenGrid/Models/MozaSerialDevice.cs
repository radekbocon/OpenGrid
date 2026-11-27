namespace OpenGrid.Models;

/// <summary>
/// A MOZA Racing serial device with RPM LED telemetry support.
/// </summary>
public class MozaSerialDevice : ISerialDevice, IRpmLedTelemetryDevice
{
    public required string Id { get; init; }
    public DeviceType DeviceType { get; init; } = DeviceType.MozaSerial;
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

    public DeviceConnectionStatus Status { get; set; }

    public MozaDeviceCategory Category { get; init; }

    /// <summary>
    /// Opt in to driving this device's LEDs from game telemetry.
    /// </summary>
    public bool DriveLedsWithTelemetry { get; set; } = true;

    /// <summary>
    /// Number of physical LEDs in one frame.
    /// </summary>
    public int RpmLedCount { get; set; } = 10;

    /// <summary>
    /// Preset used when no game or car specific LED override matches.
    /// </summary>
    public string? DefaultLedPresetId { get; set; }

    /// <summary>
    /// Game and/or car specific LED presets. The most specific match wins.
    /// </summary>
    public List<LedPresetOverride> LedOverrides { get; set; } = [];

    /// <summary>
    /// Whether this device should actively drive RPM LEDs from telemetry.
    /// </summary>
    public bool IsRpmLedActive()
    {
        if (!IsEnabled || !DriveLedsWithTelemetry)
        {
            return false;
        }

        // Wheel LED commands are routed to the wheelbase / hub serial port.
        return Category is MozaDeviceCategory.WheelBase
            or MozaDeviceCategory.Wheel
            or MozaDeviceCategory.Hub
            or MozaDeviceCategory.Unknown;
    }
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