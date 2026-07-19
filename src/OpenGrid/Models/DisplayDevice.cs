using Avalonia.Platform;

namespace OpenGrid.Models;

public class DisplayDevice : IDevice
{
    public required string Id { get; init; }
    public DeviceType DeviceType { get; init; }
    public Screen? Screen { get; set; }
    public required string Name { get; init; }
    public string? Description { get; set; }
    public bool IsEnabled { get; set; }
    public string? DashboardId { get; set; }
    public DashboardLaunchTrigger DashboardTrigger { get; set; }

    public DeviceConnectionStatus Status => Screen is not null 
        ? DeviceConnectionStatus.Connected 
        : DeviceConnectionStatus.Disconnected;
}


public enum DashboardLaunchTrigger
{
    None,
    OnAppStart,
    OnGameStart,
    OnTelemetryConnected,
}