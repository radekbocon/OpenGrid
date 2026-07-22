namespace OpenGrid.Models;

public class SoundDevice : IDevice
{
    public required string Id { get; init; }
    public DeviceType DeviceType { get; init; } = DeviceType.Sound;
    public required string Name { get; init; }
    public string? Description { get; set; }
    public bool IsEnabled { get; set; }
    public DeviceConnectionStatus Status { get; set; }
}
