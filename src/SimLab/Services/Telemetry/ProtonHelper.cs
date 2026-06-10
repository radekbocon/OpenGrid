using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Serilog;

namespace SimLab.Services.Telemetry;

public static class ProtonHelper
{
    private static readonly string[] SteamInstallPaths =
    [
        ".steam/steam",
        ".local/share/Steam",
        ".var/app/com.valvesoftware.Steam/.local/share/Steam",
    ];

    private static readonly string[] WineBinaryCandidates =
    [
        "files/bin-wow64/wine",
        "files/bin/wine",
        "dist/bin/wine",
    ];

    private static readonly string[] WineserverBinaryCandidates =
    [
        "files/bin-wow64/wineserver",
        "files/bin/wineserver",
        "dist/bin/wineserver",
    ];

    public static string? FindCompatDataDir(int appId)
    {
        foreach (var libraryPath in EnumerateLibraryFolders())
        {
            var compatData = Path.Combine(libraryPath, "steamapps", "compatdata", appId.ToString());
            if (Directory.Exists(compatData))
                return compatData;
        }

        return null;
    }

    public static string? FindProtonWine(string compatDataDir)
    {
        var toolRoot = ReadProtonToolRoot(compatDataDir);
        if (toolRoot == null)
        {
            Log.Warning("No Proton tool root found in {CompatDataDir}.", compatDataDir);
            return null;
        }

        var winePath = TryFindBinaryInDir(toolRoot, WineBinaryCandidates);
        if (winePath == null)
        {
            Log.Warning("Proton wine binary not found in {ToolRoot}.", toolRoot);
        }

        return winePath;
    }

    public static string? FindProtonWineserver(string compatDataDir)
    {
        var toolRoot = ReadProtonToolRoot(compatDataDir);
        if (toolRoot == null)
            return null;

        return TryFindBinaryInDir(toolRoot, WineserverBinaryCandidates);
    }

    private static string? ReadProtonToolRoot(string compatDataDir)
    {
        var configInfoPath = Path.Combine(compatDataDir, "config_info");
        if (!File.Exists(configInfoPath))
            return null;

        try
        {
            var lines = File.ReadAllLines(configInfoPath);
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.Length > 0 && Directory.Exists(line))
                {
                    var dir = line;
                    while (dir != null)
                    {
                        if (Directory.Exists(Path.Combine(dir, "files")))
                            return dir;

                        var parent = Path.GetDirectoryName(dir);
                        if (parent == null || parent == dir)
                            break;
                        dir = parent;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to read config_info at {Path}", configInfoPath);
        }

        return null;
    }

    private static IEnumerable<string> EnumerateLibraryFolders()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        foreach (var relative in SteamInstallPaths)
        {
            var path = Path.Combine(homeDir, relative);
            var realPath = ResolveSymlink(path);
            if (realPath != null && seen.Add(realPath))
                yield return realPath;
        }

        foreach (var root in new List<string>(seen))
        {
            var vdfPath = Path.Combine(root, "steamapps", "libraryfolders.vdf");
            foreach (var libPath in ParseLibraryFolders(vdfPath))
            {
                if (seen.Add(libPath))
                    yield return libPath;
            }
        }
    }

    private static IEnumerable<string> ParseLibraryFolders(string vdfPath)
    {
        if (!File.Exists(vdfPath))
            yield break;

        string[] lines;
        try
        {
            lines = File.ReadAllLines(vdfPath);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to read libraryfolders.vdf at {Path}", vdfPath);
            yield break;
        }

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            var match = Regex.Match(trimmed, @"^""(?:path|\d+)""\s+""(.+)""$");
            if (match.Success)
            {
                var path = match.Groups[1].Value;
                if (Directory.Exists(path))
                    yield return path;
            }
        }
    }

    private static string? TryFindBinaryInDir(string toolDir, string[] candidates)
    {
        if (!Directory.Exists(toolDir))
            return null;

        foreach (var relative in candidates)
        {
            var candidate = Path.Combine(toolDir, relative);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    private static string? ResolveSymlink(string path)
    {
        try
        {
            if (!Directory.Exists(path))
                return null;

            var info = new DirectoryInfo(path);
            if (info.LinkTarget != null)
            {
                var target = info.LinkTarget;
                if (!Path.IsPathRooted(target))
                    target = Path.Combine(info.Parent!.FullName, target);

                return Path.GetFullPath(target);
            }

            return Path.GetFullPath(path);
        }
        catch
        {
            return null;
        }
    }
}
