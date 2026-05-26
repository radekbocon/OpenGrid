using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using SimLab.Controls;
using SimLab.Models;
using SimLab.Services;
using SkiaSharp;

namespace SimLab.ViewModels;

public partial class SessionDetailsViewModel : LapChartViewModelBase
{
    private static readonly SKColor DefaultColor = SKColors.DodgerBlue;
    
    private readonly SessionRepository _sessionRepository;
    private readonly INavigationService _navigationService;
    private Session? _session;

    [ObservableProperty]
    public partial Lap? SelectedLap { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<Lap>? Laps { get; set; }

    [ObservableProperty]
    public partial string? LapTime { get; set; }

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
            var viewModel = new LapSelectionViewModel(_sessionRepository, _navigationService, _session);
            var dialog = new LapSelectionDialog { DataContext = viewModel };
            DialogHost.Show(dialog);
        }
    }

    partial void OnSelectedLapChanged(Lap? value)
    {
        if (value is null)
        {
            return;
        }

        SetCharts(value);

        LapTime = $@"Time: {SelectedLap?.Time:mm\:ss\.fff}";
    }
    
    private void SetCharts(Lap lap)
    {
        Task.Run(() =>
        {
            var distanceOffset = lap.Records.First().Distance;
            var xs = lap.Records.Select(r => (double)(r.Distance - distanceOffset)).ToArray();  

            var gasData = CreateDownsampledChartData("Gas", xs, lap.Records.Select(r => (double)r.Gas), DefaultColor);
            var brakeData =
                CreateDownsampledChartData("Brake", xs, lap.Records.Select(r => (double)r.Brake), DefaultColor);
            var steeringData = CreateDownsampledChartData("Steering", xs,
                lap.Records.Select(r => (double)r.SteerAngle), DefaultColor);
            var speedData = CreateDownsampledChartData("Speed", xs,
                lap.Records.Select(r => Math.Round(r.SpeedKmh, 1)), DefaultColor);
            var gearData = CreateDownsampledChartData("Gear", xs, lap.Records.Select(r => (double)r.CurrentGear),
                DefaultColor, isStep: true);
            var rpmData = CreateDownsampledChartData("RPM", xs, lap.Records.Select(r => (double)r.EngineRpm),
                DefaultColor);
            
            DataMinX = xs[0];
            DataMaxX = xs[^1];
            MinX = DataMinX;
            MaxX = DataMaxX;

            Gas = [gasData];
            Brake = [brakeData];
            Steering = [steeringData];
            Speed = [speedData];
            Gear = [gearData];
            Rpm = [rpmData];
        });
    }
}
