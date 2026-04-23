using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SimLab.Models;

namespace SimLab.Services.SharedMemory;

public class SharedMemoryBridgeLauncher
{
    private Process? _bridgeProcess;
    
    public async Task LaunchBridgeAsync(SteamGame steamGame, CancellationToken cancellationToken)
    {
        try
        {
            var bridgeExePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Dokumenty/GitHub/SimLab/SimLabBridge/bin/Release/net8.0-windows/win-x64/SimLabBridge.exe"
            );

            if (!File.Exists(bridgeExePath))
            {
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
        catch (Exception)
        {
            // ignored
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