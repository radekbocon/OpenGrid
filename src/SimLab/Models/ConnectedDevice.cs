using System;

namespace SimLab.Models;

public class ConnectedDevice
{
    public required string IpAddress { get; init; }
    public string? Name { get; set; }
    public DateTime FirstSeen { get; init; } = DateTime.UtcNow;
    public DateTime LastSeen { get; init; } = DateTime.UtcNow;
}
