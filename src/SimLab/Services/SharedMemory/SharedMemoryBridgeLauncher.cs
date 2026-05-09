using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using SimLab.Models;

namespace SimLab.Services.SharedMemory;

public class SharedMemoryBridgeLauncher
{
    private Process? _bridgeProcess;
    
    public async Task LaunchBridgeAsync(SteamGame steamGame, CancellationToken cancellationToken)
    {
        try
        {
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
                await Task.Delay(2000);
            }
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
            _bridgeProcess.Kill();
            _bridgeProcess.Dispose();
            _bridgeProcess = null;
        }
    }
}