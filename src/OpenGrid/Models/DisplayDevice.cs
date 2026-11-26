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
    /// <summary>Dashboard used when no game/car override matches.</summary>
    public string? DashboardId { get; set; }
    public DashboardLaunchTrigger DashboardTrigger { get; set; }
    public List<DashboardPresetOverride> DashboardOverrides { get; set; } = [];

    public DeviceConnectionStatus Status => Screen is not null 
        ? DeviceConnectionStatus.Connected 
        : DeviceConnectionStatus.Disconnected;
}

/// <summary>
/// Game/car targeted dashboard selection. The dashboard id is stored in
/// <see cref="PresetOverride.PresetId"/> so dashboard overrides use the same
/// matching and resolution rules as bass shaker and LED overrides.
/// </summary>
public class DashboardPresetOverride : PresetOverride
{
}


public enum DashboardLaunchTrigger
{
    None,
    OnAppStart,
    OnGameStart,
    OnTelemetryConnected,
}
