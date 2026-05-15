using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using SimLab.Models;

namespace SimLab.Services;

public interface IGameService
{
    event EventHandler<GameProcessEventArgs>? GameProcessChanged;
    Task LaunchAndConnectAsync(GameItem gameItem);
    Task ConnectAsync(GameItem gameItem);
    void Cancel();
    List<GameItem> DetectGames();
}

public enum GameProcessStatus
{
    None,
    StartingGame,
    StartedGame,
    Connecting,
    Connected,
    Error
}

public class GameProcessEventArgs : EventArgs
{
    public GameProcessStatus Status { get; }
    public GameItem GameItem { get; }
    
    public GameProcessEventArgs(GameProcessStatus status, GameItem gameItem)
    {
        Status = status;
        GameItem = gameItem;
    }
}

public class GameService : IGameService
{
    private readonly ITelemetryService _telemetryService;
    private CancellationTokenSource? _cts;
    private List<GameItem> _games = [];

    public GameProcessStatus GameProcessStatus { get; private set; }
    public GameItem? CurrentGame { get; private set; }
    
    public event EventHandler<GameProcessEventArgs>? GameProcessChanged; 

    public GameService(ITelemetryService telemetryService)
    {
        _telemetryService = telemetryService;
        Task.Run(PollCurrentGameProcessAsync);
    }
    
    public List<GameItem> DetectGames()
    {
        var installed = GetInstalledGameIds();
        var running = GetRunningGameProcesses();

        _games = new List<GameItem>();

        foreach (var game in SteamGame.GetAll().Where(g => g != SteamGame.Debug))
        {
            var isInstalled = installed.Contains(game.AppId);
            var isRunning = running.Contains(game.AppId);
            _games.Add(new GameItem(game, isInstalled, isRunning));
        }

#if DEBUG
        _games.Add(new GameItem(SteamGame.Debug, true, true));
#endif
        
        return _games;
    }

    public async Task ConnectAsync(GameItem gameItem)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        Log.Information("Connecting to {Game}", gameItem.Game);
        OnGameProcessChanged(GameProcessStatus.Connecting, gameItem);
        await _telemetryService.ConnectAsync(gameItem.Game, _cts.Token);

        if (!_cts.Token.IsCancellationRequested)
        {
            _telemetryService.StartReading();
            OnGameProcessChanged(GameProcessStatus.Connected, gameItem);
        }
        else
        {
            OnGameProcessChanged(GameProcessStatus.None, gameItem);
        }
    }

    public async Task LaunchAndConnectAsync(GameItem gameItem)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = $"steam://rungameid/{gameItem.Game.AppId}",
                UseShellExecute = true
            });

            OnGameProcessChanged(GameProcessStatus.StartingGame, gameItem);

            while (!_cts.Token.IsCancellationRequested)
            {
                if (IsProcessRunning(gameItem.Game.ProcessName))
                {
                    gameItem.IsRunning = true;
                    break;
                }

                await Task.Delay(1000, _cts.Token);
            }

            _cts.Token.ThrowIfCancellationRequested();

            OnGameProcessChanged(GameProcessStatus.Connecting, gameItem);
            await ConnectAsync(gameItem);
            OnGameProcessChanged(GameProcessStatus.Connected, gameItem);
        }
        catch (OperationCanceledException)
        {
            OnGameProcessChanged(GameProcessStatus.None, gameItem);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to launch game");
            OnGameProcessChanged(GameProcessStatus.Error, gameItem);
        }
    }
    
    public void Cancel()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
    
    private List<int> GetInstalledGameIds()
    {
        var installed = new List<int>();
        var steamPaths = GetSteamLibraryPaths();

        foreach (var steamAppsPath in steamPaths)
        {
            if (!Directory.Exists(steamAppsPath))
                continue;

            foreach (var file in Directory.GetFiles(steamAppsPath, "appmanifest_*.acf"))
            {
                try
                {
                    var content = File.ReadAllText(file);
                    var match = Regex.Match(content, "\"appid\"\\s+\"(\\d+)\"");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out var appId))
                    {
                        installed.Add(appId);
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Failed to read manifest {File}", file);
                }
            }
        }

        return installed;
    }

    private List<string> GetSteamLibraryPaths()
    {
        var paths = new List<string>();
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var defaultSteamApps = Path.Combine(home, ".steam", "steam", "steamapps");
        if (Directory.Exists(defaultSteamApps))
        {
            paths.Add(defaultSteamApps);

            var libraryFolders = Path.Combine(defaultSteamApps, "libraryfolders.vdf");
            if (File.Exists(libraryFolders))
            {
                try
                {
                    var content = File.ReadAllText(libraryFolders);
                    var matches = Regex.Matches(content, "\"path\"\\s+\"([^\"]+)\"");
                    foreach (Match match in matches)
                    {
                        var libPath = match.Groups[1].Value.Replace("\\\\", "/");
                        var altSteamApps = Path.Combine(libPath, "steamapps");
                        if (Directory.Exists(altSteamApps))
                        {
                            paths.Add(altSteamApps);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Failed to read libraryfolders.vdf");
                }
            }
        }

        var altSteam = Path.Combine(home, ".local", "share", "Steam", "steamapps");
        if (Directory.Exists(altSteam) && !paths.Contains(altSteam))
        {
            paths.Add(altSteam);
        }

        return paths;
    }

    private bool IsProcessRunning(string processName)
    {
        if (string.IsNullOrEmpty(processName))
            return false;

        try
        {
            if (Process.GetProcessesByName(processName) is { Length: > 0 })
            {
                return true;
            }

            if (!processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                Process.GetProcessesByName(processName + ".exe") is { Length: > 0 })
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to check process {Process}", processName);
        }

        return false;
    }

    private List<int> GetRunningGameProcesses()
    {
        var running = new List<int>();
        foreach (var game in SteamGame.GetAll())
        {
            if (game.AppId == 0 || string.IsNullOrEmpty(game.ProcessName))
                continue;

            try
            {
                if (IsProcessRunning(game.ProcessName))
                    running.Add(game.AppId);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Failed to check process {Process}", game.ProcessName);
            }
        }

        return running;
    }

    private void OnGameProcessChanged(GameProcessStatus status, GameItem gameItem)
    {
        GameProcessStatus = status;
        CurrentGame = gameItem;
        GameProcessChanged?.Invoke(this, new GameProcessEventArgs(status, gameItem));
        Log.Information("Game process status changed to {Status}", status);
    }
    
    private async Task PollCurrentGameProcessAsync()
    {
        while (true)
        {
            // Game was closed
            if (CurrentGame is not null && CurrentGame is not { Game.AppId: 0 } && GameProcessStatus >= GameProcessStatus.StartedGame)
            {
                var isRunning = IsProcessRunning(CurrentGame.Game.ProcessName);
                if (!isRunning)
                {
                    _cts?.Cancel();
                    _cts?.Dispose();
                    _cts = null;
                    CurrentGame.IsRunning = false;
                    OnGameProcessChanged(GameProcessStatus.None, CurrentGame);
                    CurrentGame = null;
                }
            }

            // Game was started externally
            if (CurrentGame is null)
            {
                var runningGames = GetRunningGameProcesses();

                if (runningGames.Count > 0)
                {
                    var gameItem = _games.FirstOrDefault(x => runningGames.Contains(x.Game.AppId));

                    if (gameItem is not null)
                    {
                        CurrentGame = gameItem;
                        CurrentGame.IsRunning = true;
                        OnGameProcessChanged(GameProcessStatus.StartedGame, gameItem);
                    }
                }
            }
            await Task.Delay(2000);
        }
        // ReSharper disable once FunctionNeverReturns
    }
}