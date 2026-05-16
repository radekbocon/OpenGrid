using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SimLab.Models;

namespace SimLab.Services;

public sealed class SteamWatcher : IDisposable
{
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);
    private readonly ConcurrentDictionary<int, SteamGameProcess> _installedGames = new();
    private readonly ConcurrentDictionary<int, SteamGameProcess> _runningGames = new();
    private readonly CancellationTokenSource _cts = new();
    
    public event Action<SteamGameProcess>? GameStarted;
    public event Action<SteamGameProcess>? GameStopped;

    public List<SteamGameProcess> GetInstalledGames()
    {
        var libraries = DiscoverSteamLibraries();

        LoadInstalledGames(libraries);
        
        return _installedGames.Values.ToList();
    }
    
    public void Start()
    {
        var libraries = DiscoverSteamLibraries();
        LoadInstalledGames(libraries);
        DetectRunningGames();
        Task.Run(ProcessMonitorLoop);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    private static List<string> DiscoverSteamLibraries()
    {
        var libraries = new HashSet<string>();

        var home = Environment.GetFolderPath(
            Environment.SpecialFolder.UserProfile);

        var possibleSteamRoots = new[]
        {
            Path.Combine(home, ".local/share/Steam"),
            Path.Combine(home, ".steam/steam")
        };

        foreach (var root in possibleSteamRoots)
        {
            if (!Directory.Exists(root))
                continue;

            libraries.Add(root);

            var vdfPath = Path.Combine(
                root,
                "steamapps",
                "libraryfolders.vdf");

            if (!File.Exists(vdfPath))
            {
                continue;
            }

            var content = File.ReadAllText(vdfPath);
            var extraLibraries = ParseLibraryFoldersVdf(content);

            foreach (var lib in extraLibraries)
            {
                libraries.Add(lib);
            }
        }

        return libraries.ToList();
    }

    private static List<string> ParseLibraryFoldersVdf(string content)
    {
        var libraries = new List<string>();
        var lines = content.Split('\n');

        var depth = 0;
        var insideLibraryBlock = false;
        var insideLibraryFolders = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith("//") || line.StartsWith("/*"))
                continue;

            if (line == "{")
            {
                depth++;
                continue;
            }

            if (line == "}")
            {
                depth--;
                if (insideLibraryBlock && depth == 1)
                    insideLibraryBlock = false;
                continue;
            }

            if (depth == 0 && line.StartsWith("\"libraryfolders\""))
            {
                insideLibraryFolders = true;
                continue;
            }

            if (!insideLibraryFolders)
                continue;

            // Inside libraryfolders, look for quoted index blocks like "0" { ... }
            if (depth == 1 && line.EndsWith("{") && line.Contains('"'))
            {
                insideLibraryBlock = true;
                continue;
            }

            if (insideLibraryBlock && depth == 2 && line.StartsWith("\"path\""))
            {
                var match = Regex.Match(line, "\"path\"\\s+\"(.+)\"");
                if (match.Success)
                {
                    var path = match.Groups[1].Value;
                    // Unescape VDF escape sequences
                    path = path.Replace("\\\\", "\\").Replace("\\\"", "\"");
                    libraries.Add(path);
                }
            }
        }

        return libraries;
    }

    private void LoadInstalledGames(IEnumerable<string> libraries)
    {
        foreach (var library in libraries)
        {
            var steamApps = Path.Combine(library, "steamapps");

            if (!Directory.Exists(steamApps))
                continue;

            var manifests = Directory.GetFiles(
                steamApps,
                "appmanifest_*.acf");

            foreach (var manifest in manifests)
            {
                var game = ParseManifest(manifest, library);

                if (game == null)
                {
                    continue;
                }

                _installedGames.TryAdd(game.SteamGame.AppId, game);
            }
        }
    }

    private static SteamGameProcess? ParseManifest(string manifestPath, string library)
    {
        try
        {
            var fileName = Path.GetFileName(manifestPath);
            var match = Regex.Match(fileName, @"\d+");
            var appId = int.Parse(match.Value);
            
            var steamGame = SteamGame.GetByAppId(appId);

            if (steamGame == null)
            {
                return null;
            }
            
            return new SteamGameProcess
            {
                SteamGame = steamGame,
                LibraryPath = library
            };
        }
        catch
        {
            return null;
        }
    }

    private async Task ProcessMonitorLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                DetectRunningGames();
            }
            catch
            {
                // ignored
            }

            try
            {
                await Task.Delay(_pollInterval, _cts.Token);
            }
            catch
            {
                return;
            }
        }
    }

    private void DetectRunningGames()
    {
        var running = new HashSet<int>();
        var gameThreadProcesses = new List<Process>();

        foreach (var kvp in _installedGames)
        {
            var steamGame = kvp.Value.SteamGame;
            var processName = steamGame.ProcessName;
            Process? process;

            if (processName == "GameThread")
            {
                if (gameThreadProcesses.Count == 0)
                    gameThreadProcesses.AddRange(Process.GetProcessesByName("GameThread"));

                var matched = gameThreadProcesses.FirstOrDefault(p =>
                    MatchesGameThreadProcess(p, steamGame.InstallDirectory));

                if (matched != null)
                    gameThreadProcesses.Remove(matched);

                process = matched;
            }
            else
            {
                process = Process.GetProcessesByName(processName).FirstOrDefault();
            }

            if (process != null)
            {
                running.Add(kvp.Key);
            }
        }

        foreach (var appId in running)
        {
            if (_runningGames.TryAdd(appId, _installedGames[appId]))
            {
                GameStarted?.Invoke(_installedGames[appId]);
            }
        }

        foreach (var appId in _runningGames.Keys.ToList())
        {
            if (!running.Contains(appId) && _runningGames.TryRemove(appId, out var game))
            {
                GameStopped?.Invoke(game);
            }
        }
    }

    private static bool MatchesGameThreadProcess(Process process, string installDir)
    {
        var cmdLine = GetProcessCommandLine(process.Id);
        return cmdLine != null && cmdLine.Contains(installDir);
    }

    private static string? GetProcessCommandLine(int pid)
    {
        try
        {
            return File.ReadAllText($"/proc/{pid}/cmdline").Replace('\0', ' ');
        }
        catch
        {
            return null;
        }
    }
}