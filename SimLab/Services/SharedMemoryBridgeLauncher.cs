using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using SimLab.Models;

namespace SimLab.Services;

public class SharedMemoryBridgeLauncher
{
    private Process? _bridgeProcess;

    public event EventHandler<BridgeLauncherEventArgs>? ConnectionStatusChanged; 

    public string? ConnectionStatus { get; private set; }
    
    public async Task LaunchBridgeAsync(SteamGame steamGame)
    {
        try
        {
            var bridgeExePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Dokumenty/GitHub/SimLab/SimLabBridge/bin/Release/net8.0-windows/win-x64/SimLabBridge.exe"
            );

            if (!File.Exists(bridgeExePath))
            {
                UpdateConnectionStatus($"Bridge executable not found at {bridgeExePath}");
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

            _bridgeProcess = Process.Start(startInfo);
            if (_bridgeProcess != null)
            {
                // Wait a bit for the bridge to establish connection
                await Task.Delay(2000);
                UpdateConnectionStatus("Bridge started");
            }
        }
        catch (Exception ex)
        {
            UpdateConnectionStatus($"Failed to launch bridge: {ex.Message}");
        }
    }

    public void StopBridge()
    {
        if (_bridgeProcess is { HasExited: false })
        {
            _bridgeProcess.Kill();
            _bridgeProcess.Dispose();
            _bridgeProcess = null;
            UpdateConnectionStatus("Bridge stopped");
        }
    }

    private void UpdateConnectionStatus(string connectionStatus)
    {
        if (ConnectionStatus == connectionStatus)
        {
            return;
        }
        
        ConnectionStatus = connectionStatus;
        ConnectionStatusChanged?.Invoke(this, new BridgeLauncherEventArgs { ConnectionStatus = ConnectionStatus});
    }
}

public class BridgeLauncherEventArgs : EventArgs
{
    public required string ConnectionStatus { get; init; }
}