using System.Collections.Generic;

namespace SimLab.Models;

public record SteamGame(string Name, int AppId, bool RequiresSharedMemoryBridge, TelemetryClientType TelemetryClientType, string ProcessName, string InstallFolderName)
{
    public static SteamGame Debug => new("Debug", 0, false, TelemetryClientType.SharedMemory, "", "");
    public static SteamGame Ac => new("AC", 244210, true, TelemetryClientType.SharedMemory, "acs", "assettocorsa");
    public static SteamGame Acc => new("ACC", 805550, true, TelemetryClientType.SharedMemory, "ACC", "assettocorsacompetizione");
    public static SteamGame AcRally => new("AC Rally", 3917090, true, TelemetryClientType.SharedMemory, "ACCRally", "assettocorsarally");
    public static SteamGame AcEvo => new("AC Evo", 3058630, true, TelemetryClientType.SharedMemory, "assettocorsaevo", "assettocorsaevo");

    public static List<SteamGame> GetAll() => [ Debug, Ac, Acc, AcRally, AcEvo];
}

public enum TelemetryClientType
{
    None,
    SharedMemory,
    Udp
}