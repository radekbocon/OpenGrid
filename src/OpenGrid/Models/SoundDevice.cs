namespace OpenGrid.Models;

public class SoundDevice : IDevice
{
    public required string Id { get; init; }
    public DeviceType DeviceType { get; init; } = DeviceType.Sound;
    public required string Name { get; init; }
    public string? Description { get; set; }
    public bool IsEnabled { get; set; }
    public DeviceConnectionStatus Status { get; set; }

    /// <summary>
    /// Preset used when no game or car specific override matches.
    /// </summary>
    public string? DefaultBassShakerPresetId { get; set; }

    /// <summary>
    /// Game and/or car specific presets. The most specific match wins.
    /// </summary>
    public List<BassShakerPresetOverride> BassShakerOverrides { get; set; } = [];

    /// <summary>
    /// Legacy inline configuration. It is migrated to a preset when the device is loaded.
    /// </summary>
    public BassShakerConfiguration? BassShaker { get; set; }
}
