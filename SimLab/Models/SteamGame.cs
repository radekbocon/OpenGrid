using System.Collections.Generic;

namespace SimLab.Models;

public record SteamGame(string Name, int AppId, bool RequiresSharedMemoryBridge)
{
    public static SteamGame Ac => new("AC", 244210, true);
    public static SteamGame Acc => new("ACC", 805550, true);
    public static SteamGame AcRally => new("AC Rally", 3917090, true);
    public static SteamGame AcEvo => new("AC Evo", 3058630, true);

    public static List<SteamGame> GetAll() => [Ac, Acc, AcRally, AcEvo];
}