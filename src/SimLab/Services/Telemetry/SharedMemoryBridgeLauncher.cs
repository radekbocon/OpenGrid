using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using SimLab.Models;

namespace SimLab.Services.Telemetry;

public class SharedMemoryBridgeLauncher
{
    private Process? _bridgeProcess;

    public async Task LaunchBridgeAsync(SteamGame steamGame, CancellationToken cancellationToken)
    {
        try
        {
            Log.Information("Launching bridge for game: {0}", steamGame.AppId);
            var bridgeExePath = Path.Combine(AppContext.BaseDirectory, "SimLabBridge.exe");

            if (!File.Exists(bridgeExePath))
            {
                Log.Error("Bridge executable not found: {0}", bridgeExePath);
                return;
            }

            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var steamRoot = Path.Combine(homeDir, ".steam", "steam");
            var compatDataDir = Path.Combine(steamRoot, "steamapps", "compatdata", steamGame.AppId.ToString());
            var winePrefix = Path.Combine(compatDataDir, "pfx");

            var winePath = FindProtonWine(steamRoot, compatDataDir) ?? "wine";

            var startInfo = new ProcessStartInfo
            {
                FileName = winePath,
                Arguments = bridgeExePath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            startInfo.EnvironmentVariables["WINEPREFIX"] = winePrefix;
            startInfo.EnvironmentVariables["WINEFSYNC"] = "1";

            var wineserverPath = FindProtonWineserver(steamRoot, compatDataDir);
            if (wineserverPath != null)
            {
                startInfo.EnvironmentVariables["WINESERVER"] = wineserverPath;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            Log.Information("WINEPREFIX=\"{WinePrefix}\" {StartInfoFileName} {StartInfoArguments}", winePrefix, startInfo.FileName, startInfo.Arguments);

            _bridgeProcess = Process.Start(startInfo);
            if (_bridgeProcess != null)
            {
                // Wait a bit for the bridge to establish connection
                await Task.Delay(2000, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Error launching bridge: {0}", exception.Message);
            throw;
        }
    }

    private static string? FindProtonWine(string steamRoot, string compatDataDir)
    {
        var toolName = ReadProtonToolName(compatDataDir);
        if (toolName == null)
            return null;

        var toolDir = Path.Combine(steamRoot, "compatibilitytools.d", toolName);
        string[] candidates =
        [
            Path.Combine(toolDir, "files", "bin-wow64", "wine"),
            Path.Combine(toolDir, "files", "bin", "wine"),
        ];

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        Log.Warning("Proton tool '{ToolName}' not found at {Path}. Falling back to system wine.", toolName, toolDir);
        return null;
    }

    private static string? FindProtonWineserver(string steamRoot, string compatDataDir)
    {
        var toolName = ReadProtonToolName(compatDataDir);
        if (toolName == null)
            return null;

        var toolDir = Path.Combine(steamRoot, "compatibilitytools.d", toolName);
        string[] candidates =
        [
            Path.Combine(toolDir, "files", "bin-wow64", "wineserver"),
            Path.Combine(toolDir, "files", "bin", "wineserver"),
        ];

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    private static string? ReadProtonToolName(string compatDataDir)
    {
        var versionFile = Path.Combine(compatDataDir, "version");
        if (!File.Exists(versionFile))
            return null;

        var toolName = File.ReadAllText(versionFile).Trim();
        return string.IsNullOrEmpty(toolName) ? null : toolName;
    }

    public void StopBridge()
    {
        if (_bridgeProcess is { HasExited: false })
        {
            try
            {
                Log.Information("Killing bridge process");
                _bridgeProcess.Kill();
                _bridgeProcess.WaitForExit(2000);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error killing bridge process");
            }
            _bridgeProcess.Dispose();
            _bridgeProcess = null;
        }
        CleanupSharedMemoryFiles();
    }


    private static void CleanupSharedMemoryFiles()
    {
        try
        {
            TryDeleteFile(AcConstants.ShmPhysicsPath);
            TryDeleteFile(AcConstants.ShmGraphicsPath);
            TryDeleteFile(AcConstants.ShmStaticPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error cleaning up shared memory files");
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                Log.Information("Deleted shared memory file: {Path}", path);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not delete file: {Path}", path);
        }
    }
}