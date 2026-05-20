using System;
using System.Collections.Generic;
using System.Linq;
using SimLab.Services.Telemetry;

namespace SimLab.Models;

public record SteamGame(
    string Name, 
    int AppId, 
    bool RequiresSharedMemoryBridge, 
    Type TelemetryClientType,
    string? ImagePath,
    string InstallDirectory,
    string ProcessName)
{
    public static SteamGame Debug => new("Debug", 0, false, typeof(DebugTelemetryClient), null, string.Empty, string.Empty);
    
    public static SteamGame Ac => new(
        "Assetto Corsa", 
        244210, 
        true, 
        typeof(AcTelemetryClient), 
        "https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/244210/header.jpg", 
        "Assetto Corsa",
        "ac");
    
    public static SteamGame Acc => new(
        "Assetto Corsa Competizione", 
        805550, 
        true, 
        typeof(AcTelemetryClient), 
        "https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/805550/header.jpg", 
        "Assetto Corsa Competizione",
        "AC2-Win64-Shipp");
    
    public static SteamGame AcRally => new(
        "Assetto Corsa Rally", 
        3917090, 
        true, 
        typeof(AcTelemetryClient), 
        "https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/3917090/6954316f59850d5eb912464e30f5644a38131e7b/header.jpg",
        "Assetto Corsa Rally",
        "GameThread");
    
    public static SteamGame AcEvo => new(
        "Assetto Corsa Evo", 
        3058630, 
        true, 
        typeof(AcTelemetryClient), 
        "https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/3058630/header.jpg",
        "Assetto Corsa EVO",
        "GameThread");

    public static SteamGame DirtRally => new(
        "DiRT Rally",
        310560,
        false,
        typeof(DirtRallyTelemetryClient), 
        "https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/310560/header.jpg",
        "DiRT Rally",
        "DirtRally");

    public static SteamGame DirtRally2 => new(
        "DiRT Rally 2.0",
        690790,
        false,
        typeof(DirtRallyTelemetryClient), 
        "https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/690790/header.jpg",
        "DiRT Rally 2.0",
        "dirtrally2.exe");

    public static List<SteamGame> GetAllSupported() => [Ac, Acc, AcRally, AcEvo, DirtRally, DirtRally2];

    public static SteamGame? GetByAppId(int appId)
    {
        return GetAllSupported().FirstOrDefault(x => x.AppId == appId);
    }
}
