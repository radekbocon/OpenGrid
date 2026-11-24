namespace OpenGrid.Models;

/// <summary>
/// A device connected through a serial port.
/// </summary>
public interface ISerialDevice : IDevice
{
    /// <summary>
    /// Stable path to the port (/dev/serial/by-id/... when available).
    /// </summary>
    string PortName { get; }

    string? SerialNumber { get; }
    ushort VendorId { get; }
    ushort ProductId { get; }

    new DeviceConnectionStatus Status { get; set; }
}
