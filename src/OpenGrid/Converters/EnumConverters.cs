using Avalonia.Data.Converters;
using OpenGrid.Models;

namespace OpenGrid.Converters;

public static class EnumConverters
{
    public static readonly FuncValueConverter<DashboardLaunchTrigger, string> DashboardLaunchTriggerToString = new (
        x =>
        {
            return x switch
            {
                DashboardLaunchTrigger.None => "None",
                DashboardLaunchTrigger.OnAppStart => "Application Start",
                DashboardLaunchTrigger.OnGameStart => "Game Start",
                DashboardLaunchTrigger.OnTelemetryConnected => "Game Connected",
                _ => throw new ArgumentOutOfRangeException(nameof(x), x, string.Empty),
            };
        });
}