namespace OpenGrid.Models;

public interface IDevice
{
    string Id { get; }
    DeviceType DeviceType { get; }
    string Name { get; }
    string? Description { get; set; }
    bool IsEnabled { get; set; }
    DeviceConnectionStatus Status { get; }
}

public enum DeviceConnectionStatus
{
    Disconnected,
    Connected,
}

public enum DeviceType
{
    Display,
    Serial,
    Sound,
}