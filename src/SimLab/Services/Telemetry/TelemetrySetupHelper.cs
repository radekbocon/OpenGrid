using System;
using System.Threading.Tasks;
using DialogHostAvalonia;
using SimLab.Controls;
using SimLab.Models;

namespace SimLab.Services.Telemetry;

public static class TelemetrySetupHelper
{
    public static bool AdditionalSetupNeeded(SteamGame? game)
    {
        return IsDirtRallyGame(game);
    }

    public static async Task HandleTelemetrySetupAsync(SteamGame? steamGame)
    {
        if (IsDirtRallyGame(steamGame))
        {
            var dialog = new DirtTelemetrySetupDialog(steamGame!);
            await DialogHost.Show(dialog);
        }
    }
    
    private static bool IsDirtRallyGame(SteamGame? game) => game == SteamGame.DirtRally || game == SteamGame.DirtRally2;
}
