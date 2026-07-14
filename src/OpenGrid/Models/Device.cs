using Avalonia.Platform;

namespace OpenGrid.Models;

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

public interface IDevice
{
    string Id { get; }
    DeviceType DeviceType { get; }
    string Name { get; }
    string? Description { get; set; }
    bool IsEnabled { get; set; }
    DeviceConnectionStatus Status { get; }
}

public class DisplayDevice : IDevice
{
    public required string Id { get; init; }
    public DeviceType DeviceType { get; init; }
    public Screen? Screen { get; set; }
    public required string Name { get; init; }
    public string? Description { get; set; }
    public bool IsEnabled { get; set; }

    public DeviceConnectionStatus Status => Screen is not null 
        ? DeviceConnectionStatus.Connected 
        : DeviceConnectionStatus.Disconnected;
}