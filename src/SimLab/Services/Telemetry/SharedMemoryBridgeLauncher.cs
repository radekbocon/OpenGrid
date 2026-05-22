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

            var startInfo = new ProcessStartInfo
            {
                FileName = "protontricks-launch",
                Arguments = $"--appid {steamGame.AppId} {bridgeExePath}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

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