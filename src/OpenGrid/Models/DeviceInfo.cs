using System.Text.Json.Serialization;

namespace OpenGrid.Models;

public enum DeviceType
{
    Serial,
    Network,
    UsbHid,
    Display
}

public enum DeviceConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    Error
}

public class DeviceInfo
{
    public required string Id { get; init; }
    public required string Name { get; set; }
    public required DeviceType Type { get; init; }
    public required string ConnectionString { get; set; }
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;

    [JsonIgnore]
    public DeviceConnectionStatus Status { get; set; } = DeviceConnectionStatus.Disconnected;

    public Dictionary<string, string> Configuration { get; set; } = [];
}
