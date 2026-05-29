using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DialogHostAvalonia;
using SimLab.Controls;
using SimLab.Models;

namespace SimLab.Services.Telemetry;

public static class TelemetrySetupHelper
{
    public static bool AdditionalSetupNeeded(SteamGame? game)
    {
        if (IsDirtRallyGame(game))
        {
            return true;
        }

        if (OperatingSystem.IsLinux() && IsAcGame(game))
        {
            return true;
        }

        return false;
    }

    public static async Task HandleTelemetrySetupAsync(SteamGame? steamGame)
    {
        if (IsDirtRallyGame(steamGame))
        {
            var dialog = new DirtTelemetrySetupDialog(steamGame!);
            await DialogHost.Show(dialog);
        }
        else if (OperatingSystem.IsLinux() && IsAcGame(steamGame))
        {
            var dialog = new AcTelemetrySetupDialog();
            await DialogHost.Show(dialog);
        }
    }

    private static bool IsAcGame(SteamGame? game)
    {
        return game == SteamGame.Ac
            || game == SteamGame.Acc
            || game == SteamGame.AcRally
            || game == SteamGame.AcEvo;
    }
    
    private static bool IsDirtRallyGame(SteamGame? game) => game == SteamGame.DirtRally || game == SteamGame.DirtRally2;
}
