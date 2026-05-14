using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using SimLab.Models;
using SimLab.Services;

namespace SimLab.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly ITelemetryService _telemetryService;
    private readonly MainWindowViewModel _mainWindow;
    private CancellationTokenSource? _cts;

    public ObservableCollection<GameItem> Games { get; } = [];

    public HomeViewModel(ITelemetryService telemetryService, MainWindowViewModel mainWindow)
    {
        _telemetryService = telemetryService;
        _mainWindow = mainWindow;
        IsMenuItem = true;
    }

    public void OnActivated()
    {
        _mainWindow.CancelRequested += OnCancelRequested;
        DetectGames();
    }

    public void OnDeactivated()
    {
        _mainWindow.CancelRequested -= OnCancelRequested;
    }

    private void OnCancelRequested()
    {
        _cts?.Cancel();
    }

    private void DetectGames()
    {
        var installed = GetInstalledGameIds();
        var running = GetRunningGameProcesses();

        Games.Clear();

        foreach (var game in SteamGame.GetAll().Where(g => g != SteamGame.Debug))
        {
            var isInstalled = installed.Contains(game.AppId);
            var isRunning = running.Contains(game.AppId);
            Games.Add(new GameItem(game, isInstalled, isRunning));
        }
    }

    private static List<int> GetInstalledGameIds()
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

    private static List<string> GetSteamLibraryPaths()
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

    private static bool IsProcessRunning(string processName)
    {
        if (string.IsNullOrEmpty(processName))
            return false;

        try
        {
            if (Process.GetProcessesByName(processName).Length > 0)
                return true;

            if (!processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                Process.GetProcessesByName(processName + ".exe").Length > 0)
                return true;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to check process {Process}", processName);
        }

        return false;
    }

    private static List<int> GetRunningGameProcesses()
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

    [RelayCommand]
    private async Task ConnectAsync(GameItem gameItem)
    {
        if (_mainWindow.IsConnected)
            return;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        Log.Information("Connecting to {Game}", gameItem.Game);
        await _telemetryService.ConnectAsync(gameItem.Game, _cts.Token);

        if (!_cts.Token.IsCancellationRequested)
            _telemetryService.StartReading();
    }

    [RelayCommand]
    private async Task LaunchAndConnectAsync(GameItem gameItem)
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

            _mainWindow.IsWaitingForGame = true;

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

            _mainWindow.IsWaitingForGame = false;
            await ConnectAsync(gameItem);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _mainWindow.IsWaitingForGame = false;
        }
    }

    [RelayCommand]
    private void Disconnect()
    {
        _telemetryService.StopReading();
    }
}
