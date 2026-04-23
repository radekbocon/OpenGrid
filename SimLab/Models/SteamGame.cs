using System.Collections.Generic;

namespace SimLab.Models;

public record SteamGame(string Name, int AppId, bool RequiresSharedMemoryBridge, TelemetryClientType TelemetryClientType)
{
    public static SteamGame Ac => new("AC", 244210, true, TelemetryClientType.SharedMemory);
    public static SteamGame Acc => new("ACC", 805550, true, TelemetryClientType.SharedMemory);
    public static SteamGame AcRally => new("AC Rally", 3917090, true, TelemetryClientType.SharedMemory);
    public static SteamGame AcEvo => new("AC Evo", 3058630, true, TelemetryClientType.SharedMemory);

    public static List<SteamGame> GetAll() => [Ac, Acc, AcRally, AcEvo];
}

public enum TelemetryClientType
{
    None,
    SharedMemory,
    Udp
}