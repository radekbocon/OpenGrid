using System.Diagnostics;
using Serilog;
using OpenGrid.Models;

namespace OpenGrid.Services.Telemetry;

public class SharedMemoryBridgeLauncher
{
    private Process? _bridgeProcess;

    public async Task LaunchBridgeAsync(SteamGame steamGame, CancellationToken cancellationToken)
    {
        try
        {
            Log.Information("Launching bridge for game: {Id}", steamGame.AppId);
            var bridgeExePath = Path.Combine(AppContext.BaseDirectory, "OpenGridBridge.exe");

            if (!File.Exists(bridgeExePath))
            {
                Log.Error("Bridge executable not found: {Path}", bridgeExePath);
                return;
            }

            var compatDataDir = ProtonHelper.FindCompatDataDir(steamGame.AppId);
            if (compatDataDir == null)
            {
                Log.Error("Steam compatibility data not found for app {AppId}", steamGame.AppId);
                return;
            }

            var winePrefix = Path.Combine(compatDataDir, "pfx");
            var winePath = ProtonHelper.FindProtonWine(compatDataDir) ?? "wine";

            var startInfo = new ProcessStartInfo
            {
                FileName = winePath,
                Arguments = BuildArguments(bridgeExePath, steamGame),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            startInfo.EnvironmentVariables["WINEPREFIX"] = winePrefix;
            startInfo.EnvironmentVariables["WINEFSYNC"] = "1";
            startInfo.EnvironmentVariables.Remove("DOTNET_DiagnosticPorts");
            startInfo.EnvironmentVariables.Remove("DOTNET_DefaultDiagnosticPortSuspend");

            var wineserverPath = ProtonHelper.FindProtonWineserver(compatDataDir);
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
#if DEBUG
                _bridgeProcess.OutputDataReceived += OnBridgeOutputDataReceived;
                _bridgeProcess.ErrorDataReceived += OnBridgeErrorDataReceived;
                _bridgeProcess.BeginOutputReadLine();
                _bridgeProcess.BeginErrorReadLine(); 
#endif
                await Task.Delay(2000, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Ignored
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Error launching bridge: {Message}", exception.Message);
            throw;
        }
    }

    private static string BuildArguments(string bridgePath, SteamGame steamGame)
    {
        return $"\"{bridgePath}\" --appid {steamGame.AppId}";
    }

    private static void OnBridgeOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data != null)
        {
            Log.Information("Bridge stdout: {Line}", e.Data);
        }
    }

    private static void OnBridgeErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data != null)
        {
            Log.Warning("Bridge stderr: {Line}", e.Data);
        }
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
            _bridgeProcess.OutputDataReceived -= OnBridgeOutputDataReceived;
            _bridgeProcess.ErrorDataReceived -= OnBridgeErrorDataReceived;
            _bridgeProcess.Dispose();
            _bridgeProcess = null;
        }
        CleanupSharedMemoryFiles();
    }


    private static void CleanupSharedMemoryFiles()
    {
        try
        {
            TryDeleteFile(AcConstants.ShmAcPhysicsPath);
            TryDeleteFile(AcConstants.ShmAcGraphicsPath);
            TryDeleteFile(AcConstants.ShmAcStaticPath);
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