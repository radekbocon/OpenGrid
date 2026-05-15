using System.Collections.Generic;

namespace SimLab.Models;

public record SteamGame(string Name, int AppId, bool RequiresSharedMemoryBridge, TelemetryClientType TelemetryClientType, string ProcessName, string InstallFolderName, string? ImagePath)
{
    public static SteamGame Debug => new("Debug", 0, false, TelemetryClientType.SharedMemory, "", "", null);
    public static SteamGame Ac => new("Assetto Corsa", 244210, true, TelemetryClientType.SharedMemory, "acs", "assettocorsa", "https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/244210/header.jpg");
    public static SteamGame Acc => new("Assetto Corsa Competizione", 805550, true, TelemetryClientType.SharedMemory, "ACC", "assettocorsacompetizione", "https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/805550/header.jpg");
    public static SteamGame AcRally => new("Assetto Corsa Rally", 3917090, true, TelemetryClientType.SharedMemory, "ACCRally", "assettocorsarally", "https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/3917090/6954316f59850d5eb912464e30f5644a38131e7b/header.jpg");
    public static SteamGame AcEvo => new("Assetto Corsa Evo", 3058630, true, TelemetryClientType.SharedMemory, "assettocorsaevo", "assettocorsaevo", "https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/3058630/header.jpg");

    public static List<SteamGame> GetAll() => [Debug, Ac, Acc, AcRally, AcEvo];
}

public enum TelemetryClientType
{
    None,
    SharedMemory,
    Udp
}