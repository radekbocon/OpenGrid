using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using SimLab.Models;
using SimLab.Services;

namespace SimLab.ViewModels;

public partial class DashboardViewModel : ViewModelBase, IDisposable
{
    private readonly ITelemetryService _telemetryService;

    [ObservableProperty] private string _speedDisplay = "0";

    [ObservableProperty] private double _rpm;

    [ObservableProperty] private double _maxRpm;

    [ObservableProperty] private double _gas;

    [ObservableProperty] private double _brake;

    [ObservableProperty] private double _clutch;

    [ObservableProperty] private string _steerDisplay = "0°";

    [ObservableProperty] private string _fuelDisplay = "0%";

    [ObservableProperty] private string _gearDisplay = "N";

    [ObservableProperty] private string? _car;

    [ObservableProperty] private string? _track;

    [ObservableProperty] private int _currentLap;

    [ObservableProperty] private string _sessionType = "";

    [ObservableProperty] private string _lapTime = "--:--";

    [ObservableProperty] private string _tireFlDisplay = "FL: 0°C";

    [ObservableProperty] private string _tireFrDisplay = "FR: 0°C";

    [ObservableProperty] private string _tireRlDisplay = "RL: 0°C";

    [ObservableProperty] private string _tireRrDisplay = "RR: 0°C";

    public DashboardViewModel(ITelemetryService telemetryService)
    {
        _telemetryService = telemetryService;
        _telemetryService.TelemetryReceived += OnTelemetryReceived;
    }

    private void OnTelemetryReceived(object? sender, TelemetryEventArgs e)
    {
        var t = e.Telemetry;
        SpeedDisplay = t.SpeedKmh.ToString("F0");
        Rpm = t.EngineRpm;
        if (t.MaxRpm > 0)
            MaxRpm = t.MaxRpm;
        Gas = t.Gas;
        Brake = t.Brake;
        Clutch = t.Clutch;
        SteerDisplay = $"{t.SteerAngle:F1}°";
        FuelDisplay = $"{t.Fuel:F1}%";
        GearDisplay = t.CurrentGear switch
        {
            Gear.R => "R",
            Gear.N => "N",
            _ => ((int)t.CurrentGear - 1).ToString()
        };
        CurrentLap = t.CurrentLap;
        Car = t.Car ?? Car;
        Track = t.Track ?? Track;
        SessionType = t.SessionType.ToString();
        LapTime = t.LapTime > TimeSpan.Zero ? t.LapTime.ToString(@"mm\:ss\.fff") : "--:--";
        TireFlDisplay = $"FL: {t.TireTemperatures.FrontLeft:F1}°C";
        TireFrDisplay = $"FR: {t.TireTemperatures.FrontRight:F1}°C";
        TireRlDisplay = $"RL: {t.TireTemperatures.RearLeft:F1}°C";
        TireRrDisplay = $"RR: {t.TireTemperatures.RearRight:F1}°C";
    }

    public void Dispose()
    {
        _telemetryService.TelemetryReceived -= OnTelemetryReceived;
    }
}