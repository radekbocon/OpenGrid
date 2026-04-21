using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SimLab.Services;

public class SharedMemoryBridgeLauncher
{
    private Process? _bridgeProcess;

    public string ConnectionStatus { get; private set; } = "Not started";
    
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
                ConnectionStatus = $"Bridge executable not found at {bridgeExePath}";
                return;
            }

            // Use protontricks to run the bridge in the ACC Proton prefix
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
                ConnectionStatus = "Bridge started";
            }
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"Failed to launch bridge: {ex.Message}";
        }
    }
    
    public void StopBridge()
    {
        if (_bridgeProcess is { HasExited: false })
        {
            _bridgeProcess.Kill();
            _bridgeProcess.Dispose();
            _bridgeProcess = null;
            ConnectionStatus = "Bridge stopped";
        }
    }
}

public record SteamGame(string Name, int AppId)
{
    public static SteamGame Acc => new("ACC", 805550);
    public static SteamGame AcRally => new("AC Rally", 3917090);
    public static SteamGame AcEvo => new("AC Evo", 3058630);
}