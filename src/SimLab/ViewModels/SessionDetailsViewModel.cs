using System;
using System.Collections.Generic;
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

    public string Title => $"{_session?.Info.Game.Name} - {_session?.Info.Car} - {_session?.Info.Track}";
    
    [ObservableProperty]
    public partial Lap? SelectedLap { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<Lap>? Laps { get; set; }

    [ObservableProperty]
    public partial TimeSpan LapTime { get; set; }

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
            OnPropertyChanged(nameof(Title));
            Laps = session.Laps;
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

        Laps = _session.Laps;
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

        LapTime = SelectedLap?.Time ?? TimeSpan.Zero;
    }

    private void SetCharts(Lap lap)
    {
        Task.Run(() =>
        {
            var distanceOffset = lap.Records.First().Distance;
            var xs = lap.Records.Select(r => (double)(r.Distance - distanceOffset)).ToArray();

            var gasData = CreateChartData("Gas", xs, lap.Records.Select(r => (double)r.Gas), DefaultColor);
            var brakeData = CreateChartData("Brake", xs, lap.Records.Select(r => (double)r.Brake), DefaultColor);
            var steeringData = CreateChartData("Steering", xs, lap.Records.Select(r => (double)r.SteerAngle), DefaultColor);
            var speedData = CreateChartData("Speed", xs, lap.Records.Select(r => Math.Round(r.SpeedKmh, 1)), DefaultColor);
            var gearData = CreateChartData("Gear", xs, lap.Records.Select(r => (double)r.CurrentGear), DefaultColor, isStep: true);
            var rpmData = CreateChartData("RPM", xs, lap.Records.Select(r => (double)r.EngineRpm), DefaultColor);

            Subplots =
            [
                new SubplotDefinition { Title = "Gas", Series = [gasData], YLabeler = DoubleLabeler, YMinLimit = 0, YMaxLimit = 1.02 },
                new SubplotDefinition { Title = "Brake", Series = [brakeData], YLabeler = DoubleLabeler, YMinLimit = 0, YMaxLimit = 1.02 },
                new SubplotDefinition { Title = "Steering", Series = [steeringData], YLabeler = DoubleLabeler, YMinLimit = -1, YMaxLimit = 1 },
                new SubplotDefinition { Title = "Speed", Series = [speedData], YLabeler = IntegerLabeler },
                new SubplotDefinition { Title = "Gear", Series = [gearData], YLabeler = GearLabeler, YMinLimit = 0, YMaxLimit = 7 },
                new SubplotDefinition { Title = "RPM", Series = [rpmData], YLabeler = IntegerLabeler, YMinLimit = 0 },
            ];
        });
    }
}
