using System.Collections.Generic;
using System.Linq;
using SimLab.Models;

namespace SimLab.Services;

public class SteamGameManager
{
    private readonly SteamWatcher _steamWatcher;
    private List<SteamGameProcess>? _installedGames;

    public SteamGameManager(SteamWatcher steamWatcher)
    {
        _steamWatcher = steamWatcher;
        _steamWatcher.Start();
        _steamWatcher.GameStarted += SteamWatcherOnGameStarted;
        _steamWatcher.GameStopped += SteamWatcherOnGameStopped;
    }

    private void SteamWatcherOnGameStopped(SteamGameProcess gameProcess)
    {
        
    }

    private void SteamWatcherOnGameStarted(SteamGameProcess gameProcess)
    {
        
    }

    public List<SteamGameProcess> GetInstalledGames()
    {
        _installedGames ??= _steamWatcher.GetInstalledGames().ToList();
        return _installedGames;
    }
}