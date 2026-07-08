using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenGrid.Models;
using OpenGrid.Services.SessionPersist;
using OpenGrid.Services.Telemetry;
using Serilog;

namespace OpenGrid.Services;

public class SteamGameManager
{
    private readonly SteamWatcher _steamWatcher;
    private readonly ISettingsService _settingsService;
    private readonly ITelemetryService _telemetryService;
    private readonly SessionRepository _sessionRepository;
    
    private List<SteamGameProcess>? _installedGames;

    public event EventHandler<SteamGameProcess>? GameStarted;
    public event EventHandler<SteamGameProcess>? GameStopped;

    public SteamGameManager(SteamWatcher steamWatcher, ISettingsService settingsService, ITelemetryService telemetryService, SessionRepository sessionRepository)
    {
        _steamWatcher = steamWatcher;
        _settingsService = settingsService;
        _telemetryService = telemetryService;
        _sessionRepository = sessionRepository;
        
        _steamWatcher.Start();
        _steamWatcher.GameStarted += SteamWatcherOnGameStarted;
        _steamWatcher.GameStopped += SteamWatcherOnGameStopped;
    }

    private void SteamWatcherOnGameStopped(SteamGameProcess gameProcess)
    {
        GameStopped?.Invoke(this, gameProcess);
    }

    private void SteamWatcherOnGameStarted(SteamGameProcess gameProcess)
    {
        OnGameStartedAsync(gameProcess).FireAndForgetSafe();
        GameStarted?.Invoke(this, gameProcess);
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
    
    private async Task OnGameStartedAsync(SteamGameProcess? game)
    {
        try
        {
            if (game?.SteamGame.AppId is not {} appId || !_settingsService.AutoConnectGameAppIds.Contains(appId))
            {
                return;
            }

            var connected = await _telemetryService.ConnectAsync(game.SteamGame);
            if (!connected)
            {
                return;
            }

            if (_settingsService.AutoRecordingGameAppIds.Contains(appId))
            {
                _sessionRepository.StartRecording();
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "AutoGameService failed to handle game start for {AppId}", game?.SteamGame.AppId);
        }
    }
}
