using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimLab.Models;
using SimLab.Services;

namespace SimLab.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly ITelemetryService _telemetryService;

    [ObservableProperty]
    public partial string SpeedDisplay { get; set; } = "0";

    [ObservableProperty]
    public partial double Rpm { get; set; }

    [ObservableProperty]
    public partial double MaxRpm { get; set; }

    [ObservableProperty]
    public partial double Gas { get; set; }

    [ObservableProperty]
    public partial double Brake { get; set; }

    [ObservableProperty]
    public partial double Clutch { get; set; }

    [ObservableProperty]
    public partial string SteerDisplay { get; set; } = "0°";

    [ObservableProperty]
    public partial string FuelDisplay { get; set; } = "0";

    [ObservableProperty]
    public partial string GearDisplay { get; set; } = "N";

    [ObservableProperty]
    public partial string? Car { get; set; }

    [ObservableProperty]
    public partial string? Track { get; set; }

    [ObservableProperty]
    public partial int CurrentLap { get; set; }

    [ObservableProperty]
    public partial string SessionType { get; set; } = "";

    [ObservableProperty]
    public partial string LapTime { get; set; } = "--:--";

    [ObservableProperty]
    public partial string TireFlDisplay { get; set; } = "FL: 0°C";

    [ObservableProperty]
    public partial string TireFrDisplay { get; set; } = "FR: 0°C";

    [ObservableProperty]
    public partial string TireRlDisplay { get; set; } = "RL: 0°C";

    [ObservableProperty]
    public partial string TireRrDisplay { get; set; } = "RR: 0°C";

    public DashboardViewModel(ITelemetryService telemetryService)
    {
        _telemetryService = telemetryService;
        _telemetryService.TelemetryReceived += OnTelemetryReceived;
    }

    [RelayCommand]
    private void Unloaded()
    {
        _telemetryService.TelemetryReceived -= OnTelemetryReceived;
    }

    private void OnTelemetryReceived(object? sender, TelemetryEventArgs e)
    {
        var t = e.Telemetry;
        SpeedDisplay = t.SpeedKmh.ToString("F0");
        Rpm = t.EngineRpm;
        MaxRpm = t.MaxRpm switch
        {
            > 0 => t.MaxRpm,
            _ => MaxRpm
        };
        Gas = t.Gas;
        Brake = t.Brake;
        Clutch = t.Clutch;
        SteerDisplay = $"{t.SteerAngle:F1}°";
        FuelDisplay = $"{t.Fuel:F1}";
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
}