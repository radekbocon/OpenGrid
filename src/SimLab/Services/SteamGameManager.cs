using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SimLab.Models;

namespace SimLab.Services;

public class SteamGameManager
{
    private readonly SteamWatcher _steamWatcher;
    private readonly HashSet<int> _runningAppIds = [];
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
        lock (_runningAppIds)
            _runningAppIds.Remove(gameProcess.SteamGame.AppId);
        GameStopped?.Invoke(gameProcess);
    }

    private void SteamWatcherOnGameStarted(SteamGameProcess gameProcess)
    {
        lock (_runningAppIds)
            _runningAppIds.Add(gameProcess.SteamGame.AppId);
        GameStarted?.Invoke(gameProcess);
    }

    public List<SteamGameProcess> GetInstalledGames()
    {
        _installedGames ??= _steamWatcher.GetInstalledGames().ToList();
        return _installedGames;
    }

    public bool IsRunning(SteamGameProcess game)
    {
        lock (_runningAppIds)
            return _runningAppIds.Contains(game.SteamGame.AppId);
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
