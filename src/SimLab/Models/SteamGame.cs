using System.Collections.Generic;
using System.Linq;

namespace SimLab.Models;

public record SteamGame(
    string Name, 
    int AppId, 
    bool RequiresSharedMemoryBridge, 
    TelemetryClientType TelemetryClientType,
    string? ImagePath,
    string InstallDirectory,
    string ProcessName)
{
    public static SteamGame Debug => new("Debug", 0, false, TelemetryClientType.SharedMemory, null, string.Empty, string.Empty);
    
    public static SteamGame Ac => new(
        "Assetto Corsa", 
        244210, 
        true, 
        TelemetryClientType.SharedMemory, 
        "https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/244210/header.jpg", 
        "Assetto Corsa",
        "ac");
    
    public static SteamGame Acc => new(
        "Assetto Corsa Competizione", 
        805550, 
        true, 
        TelemetryClientType.SharedMemory, 
        "https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/805550/header.jpg", 
        "Assetto Corsa Competizione",
        "ACC");
    
    public static SteamGame AcRally => new(
        "Assetto Corsa Rally", 
        3917090, 
        true, 
        TelemetryClientType.SharedMemory, 
        "https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/3917090/6954316f59850d5eb912464e30f5644a38131e7b/header.jpg",
        "Assetto Corsa Rally",
        "GameThread");
    
    public static SteamGame AcEvo => new(
        "Assetto Corsa Evo", 
        3058630, 
        true, 
        TelemetryClientType.SharedMemory, 
        "https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/3058630/header.jpg",
        "Assetto Corsa EVO",
        "GameThread");

    public static List<SteamGame> GetAllSupported() => [Debug, Ac, Acc, AcRally, AcEvo];

    public static SteamGame? GetByAppId(int appId)
    {
        return GetAllSupported().FirstOrDefault(x => x.AppId == appId);
    }
}

public enum TelemetryClientType
{
    None,
    SharedMemory,
    Udp
}