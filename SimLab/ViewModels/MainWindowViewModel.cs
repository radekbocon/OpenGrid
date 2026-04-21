using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
    public partial string ConnectionStatus { get; set; } = "Disconnected";

    [ObservableProperty]
    public partial string? CurrentTrack { get; set; } = "N/A";

    [ObservableProperty]
    public partial string CurrentSessionType { get; set; } = "N/A";

    [ObservableProperty]
    public partial int CurrentLap { get; set; }

    [ObservableProperty]
    public partial float CurrentSpeed { get; set; }

    [ObservableProperty]
    public partial float EngineRpm { get; set; }

    [ObservableProperty]
    public partial float FuelRemaining { get; set; }

    [ObservableProperty]
    public partial string BestLapTime { get; set; } = "N/A";

    [ObservableProperty]
    public partial string LastLapTime { get; set; } = "N/A";

    [ObservableProperty]
    public partial string CurrentLapTime { get; set; } = "N/A";

    [ObservableProperty]
    public partial bool IsConnected { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<LapInfo> RecentLaps { get; set; } = [];

    [ObservableProperty]
    public partial float ThrottleInput { get; set; }

    [ObservableProperty]
    public partial float BrakeInput { get; set; }

    [ObservableProperty]
    public partial float CurrentGear { get; set; }

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
                Arguments = $"--appid 805550 {bridgeExePath}",
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