using System;
using CommunityToolkit.Mvvm.ComponentModel;
using SimLab.Services;

namespace SimLab.Models;

public partial class GameItem : ObservableObject
{
    public SteamGame Game { get; }

    public bool IsInstalled { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText), nameof(ShowConnectButton), nameof(ShowLaunchAndConnectButton))]
    public partial GameProcessStatus Status { get; set; }

    public bool IsRunning => Status >= GameProcessStatus.StartedGame;

    public string StatusText => Status switch
    {
        GameProcessStatus.None or GameProcessStatus.Error => string.Empty,
        GameProcessStatus.StartingGame => "Launching...",
        GameProcessStatus.StartedGame => "Running",
        GameProcessStatus.Connecting => "Connecting...",
        GameProcessStatus.Connected => "Connected",
        _ => throw new ArgumentOutOfRangeException()
    };

    public bool ShowConnectButton => Status == GameProcessStatus.StartedGame;

    public bool ShowLaunchAndConnectButton => IsInstalled && Status < GameProcessStatus.StartingGame;

    public GameItem(SteamGame game, bool isInstalled, GameProcessStatus status)
    {
        Game = game;
        IsInstalled = isInstalled;
        Status = status;
    }
}
