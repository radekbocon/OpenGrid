using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimLab.Models;
using SimLab.Services;

namespace SimLab.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly SharedFileReader _reader;
    private readonly TelemetryRepository _repository;
    private Process? _bridgeProcess;

    [ObservableProperty]
    private string _connectionStatus = "Disconnected";

    [ObservableProperty]
    private string? _currentTrack = "N/A";

    [ObservableProperty]
    private string _currentSessionType = "N/A";

    [ObservableProperty]
    private int _currentLap;

    [ObservableProperty]
    private float _currentSpeed;

    [ObservableProperty]
    private float _engineRpm;

    [ObservableProperty]
    private float _fuelRemaining;

    [ObservableProperty]
    private string _bestLapTime = "N/A";

    [ObservableProperty]
    private string _lastLapTime = "N/A";

    [ObservableProperty]
    private string _currentLapTime = "N/A";

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private ObservableCollection<LapInfo> _recentLaps = new();

    [ObservableProperty]
    private float _throttleInput;

    [ObservableProperty]
    private float _brakeInput;

    [ObservableProperty]
    private float _currentGear;

    public MainWindowViewModel()
    {
        _reader = new SharedFileReader();
        _repository = new TelemetryRepository();

        _reader.TelemetryDataReceived += OnTelemetryDataReceived;
        _reader.ConnectionStatusChanged += OnConnectionStatusChanged;

        _repository.TelemetryUpdatedObservable.Subscribe(_ => UpdateTelemetryDisplay());
        _repository.LapCompletedObservable.Subscribe(lap => OnLapCompleted(lap));
    }

    [RelayCommand]
    public async Task ConnectAsync()
    {
        if (IsConnected)
        {
            await DisconnectAsync();
            return;
        }

        // Launch the bridge
        await LaunchBridgeAsync();

        // Start reading from shared files
        _reader.StartReading();
    }

    [RelayCommand]
    public async Task DisconnectAsync()
    {
        _reader.StopReading();

        // Stop the bridge
        if (_bridgeProcess != null && !_bridgeProcess.HasExited)
        {
            _bridgeProcess.Kill();
            await _bridgeProcess.WaitForExitAsync();
            _bridgeProcess.Dispose();
            _bridgeProcess = null;
        }

        IsConnected = false;
    }

    private async Task LaunchBridgeAsync()
    {
        try
        {
            // Use wine directly to run the bridge executable in the Proton prefix
            // The bridge exe should be the self-contained published version
            var bridgeExePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Dokumenty/GitHub/SimLab/SimLabBridge/bin/Release/net8.0-windows/win-x64/publish/SimLabBridge.exe"
            );

            if (!File.Exists(bridgeExePath))
            {
                ConnectionStatus = $"Bridge executable not found at {bridgeExePath}";
                return;
            }

            // Use Proton's wine instead of system wine to avoid version conflicts
            // Adjust the paths if your ACC installation uses a different appid
            var winePrefix = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".steam/steam/steamapps/compatdata/805550/pfx"
            );

            var protonToolsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".steam/steam/steamapps/Proton-Experimental/proton"
            );

            // Try to find Proton wine, fallback to system wine if not found
            string wineExe = "wine";
            if (File.Exists(protonToolsPath))
            {
                wineExe = Path.Combine(
                    Path.GetDirectoryName(protonToolsPath) ?? "",
                    "wine64"
                );
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = wineExe,
                Arguments = $"\"{bridgeExePath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            // Set environment variables
            startInfo.EnvironmentVariables["WINEPREFIX"] = winePrefix;
            startInfo.EnvironmentVariables["WINEARCH"] = "win64";

            _bridgeProcess = Process.Start(startInfo);
            if (_bridgeProcess != null)
            {
                // Wait a bit for the bridge to start
                await Task.Delay(2000);
                ConnectionStatus = "Bridge started";
            }
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"Failed to launch bridge: {ex.Message}";
        }
    }

    private void OnTelemetryDataReceived(object? sender, TelemetryDataEventArgs e)
    {
        if (e.Snapshot != null)
        {
            _repository.ProcessTelemetry(e.Snapshot);
        }
    }

    private void OnConnectionStatusChanged(object? sender, string message)
    {
        ConnectionStatus = message;
        IsConnected = _reader.IsConnected;
    }

    private void UpdateTelemetryDisplay()
    {
        var session = _repository.CurrentSession;
        if (session == null)
            return;

        CurrentTrack = session.Track ?? "Unknown";
        CurrentSessionType = session.SessionType.ToString();

        var stats = _repository.GetCurrentSessionStatistics();
        if (stats.BestLapTime.HasValue)
            BestLapTime = FormatTimeSpan(stats.BestLapTime.Value);
        if (stats.AverageLapTime.HasValue)
            LastLapTime = FormatTimeSpan(stats.AverageLapTime.Value);

        // Update recent laps
        RecentLaps.Clear();
        foreach (var lap in _repository.GetRecentLaps(5))
        {
            RecentLaps.Add(lap);
        }

        // Update live telemetry
        var snapshot = _repository.CurrentSnapshot;
        if (snapshot != null)
        {
            CurrentLap = snapshot.CurrentLap;
            CurrentSpeed = snapshot.SpeedKmh;
            EngineRpm = snapshot.EngineRpm;
            FuelRemaining = snapshot.FuelRemaining;
            CurrentGear = snapshot.CurrentGear;
            ThrottleInput = snapshot.Gas;
            BrakeInput = snapshot.Brake;
        }
    }

    private void OnLapCompleted(LapInfo lap)
    {
        if (lap.LapTime.HasValue)
        {
            LastLapTime = FormatTimeSpan(lap.LapTime.Value);
        }
    }

    private string FormatTimeSpan(TimeSpan timeSpan)
    {
        return $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}.{timeSpan.Milliseconds:D3}";
    }
}