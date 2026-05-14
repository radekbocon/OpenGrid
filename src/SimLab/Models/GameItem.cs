using CommunityToolkit.Mvvm.ComponentModel;

namespace SimLab.Models;

public partial class GameItem : ObservableObject
{
    public SteamGame Game { get; }

    public bool IsInstalled { get; }

    [ObservableProperty]
    public partial bool IsRunning { get; set; }

    public string StatusIcon => IsRunning ? "\U0001f7e2" : IsInstalled ? "\U0001f7e1" : "\U0001f534";

    public string StatusText => IsRunning ? "Running" : IsInstalled ? "Installed" : "Not installed";

    public bool ShowConnectButton => IsRunning;

    public bool ShowLaunchAndConnectButton => IsInstalled && !IsRunning && Game.AppId != 0;

    public GameItem(SteamGame game, bool isInstalled, bool isRunning)
    {
        Game = game;
        IsInstalled = isInstalled;
        IsRunning = isRunning;
    }
}
