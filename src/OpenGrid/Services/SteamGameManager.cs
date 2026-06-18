using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenGrid.Models;

namespace OpenGrid.Services;

public class SteamGameManager
{
    private readonly SteamWatcher _steamWatcher;
    private List<SteamGameProcess>? _installedGames;

    public event Action<SteamGameProcess>? GameStarted;
    public event Action<SteamGameProcess>? GameStopped;

    public SteamGameManager(SteamWatcher steamWatcher)
    {
        _steamWatcher = steamWatcher;
        _steamWatcher.Start();
        _steamWatcher.GameStarted += SteamWatcherOnGameStarted;
        _steamWatcher.GameStopped += SteamWatcherOnGameStopped;
    }

    private void SteamWatcherOnGameStopped(SteamGameProcess gameProcess)
    {
        GameStopped?.Invoke(gameProcess);
    }

    private void SteamWatcherOnGameStarted(SteamGameProcess gameProcess)
    {
        GameStarted?.Invoke(gameProcess);
    }

    public List<SteamGameProcess> GetInstalledGames()
    {
        _installedGames ??= _steamWatcher.GetInstalledGames().ToList();
        return _installedGames;
    }

    public bool IsRunning(SteamGameProcess game)
    {
        if (game.SteamGame == SteamGame.Debug)
        {
            return true;
        }
        
        return _steamWatcher.RunningGames.Contains(game);
    }

    public void LaunchGame(SteamGameProcess game)
    {
        var url = $"steam://rungameid/{game.SteamGame.AppId}";
        Process.Start(new ProcessStartInfo
        {
            FileName = "xdg-open",
            Arguments = url,
            UseShellExecute = true
        });
    }
}
