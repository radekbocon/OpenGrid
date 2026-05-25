using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView.Painting;
using SimLab.Controls;
using SimLab.Models;
using SimLab.Services;
using SkiaSharp;

namespace SimLab.ViewModels;

public partial class SessionDetailsViewModel : LapChartViewModelBase
{
    private readonly SessionRepository _sessionRepository;
    private readonly INavigationService _navigationService;
    private Session? _session;

    [ObservableProperty]
    public partial Lap? SelectedLap { get; set; }
    
    [ObservableProperty]
    public partial ObservableCollection<Lap>? Laps { get; set; } 

    [ObservableProperty]
    public partial string? LapTime { get; set; }

    private static readonly SolidColorPaint GasPaint = new(SKColors.Green, 2);
    private static readonly SolidColorPaint BrakePaint = new(SKColors.Red, 2);
    private static readonly SolidColorPaint DefaultPaint = new(SKColors.DodgerBlue, 2);

    public SessionDetailsViewModel(SessionRepository sessionRepository, INavigationService navigationService)
    {
        _sessionRepository = sessionRepository;
        _navigationService = navigationService;
    }

    public override void SetParameters(params object[] parameters)
    {
        if (parameters.Length > 0 && parameters[0] is Session { Laps.Count: > 0 } session)
        {
            _session = session;
            Laps = new ObservableCollection<Lap>(_session.Laps);
            SelectedLap = Laps.First();
        }
    }

    [RelayCommand]
    private async Task DeleteLapAsync()
    {
        if (_session is null || SelectedLap is null)
        {
            return;
        }

        var confirmDialog = new ConfirmDialog("Are you sure you want to delete this lap?");
        await DialogHost.Show(confirmDialog);

        if (!confirmDialog.Result)
        {
            return;
        }

        if (_session.Laps.Count == 1)
        {
            _sessionRepository.DeleteSession(_session);
            _navigationService.NavigateTo<SessionsViewModel>();
            return;
        }
        
        _sessionRepository.DeleteLap(_session, SelectedLap.Number);

        Laps = new ObservableCollection<Lap>(_session.Laps);
        SelectedLap = Laps.FirstOrDefault();
    }

    [RelayCommand]
    private void CompareWithOtherLaps()
    {
        if (_session is not null)
        {
            _navigationService.NavigateTo<LapSelectionViewModel>(_session);
        }
    }

    partial void OnSelectedLapChanged(Lap? value)
    {
        if (value is null)
        {
            return;
        }

        var distanceOffset = value.Records.First().Distance;
        
        var gasData = new ChartData("Gas", 
            value.Records.Select(r => new ObservablePoint(r.Distance - distanceOffset, r.Gas)), 
            GasPaint);
        var brakeData = new ChartData("Brake", 
            value.Records.Select(r => new ObservablePoint(r.Distance - distanceOffset, r.Brake)), 
            BrakePaint);
        var steeringData = new ChartData("Steering", 
            value.Records.Select(r => new ObservablePoint(r.Distance - distanceOffset, r.SteerAngle)), 
            DefaultPaint);
        var speedData = new ChartData("Speed", 
            value.Records.Select(r => new ObservablePoint(r.Distance - distanceOffset, Math.Round(r.SpeedKmh, 1))), 
            DefaultPaint);
        var gearData = new ChartData("Gear", 
            value.Records.Select(r => new ObservablePoint(r.Distance - distanceOffset, (int)r.CurrentGear)), 
            DefaultPaint);
        var rpmData = new ChartData("RPM", 
            value.Records.Select(r => new ObservablePoint(r.Distance - distanceOffset, r.EngineRpm)), 
            DefaultPaint);
        
        DataMinX = value.Records.Min(r => r.Distance - distanceOffset);
        DataMaxX = value.Records.Max(r => r.Distance - distanceOffset);
        MinX = DataMinX;
        MaxX = DataMaxX;

        Inputs = [gasData, brakeData];
        Steering = [steeringData];
        Speed = [speedData];
        Gear = [gearData];
        Rpm = [rpmData];
        
        ResampleAllCharts();
        LapTime = $@"Time: {SelectedLap?.Time:mm\:ss\.fff}";
    }
}
