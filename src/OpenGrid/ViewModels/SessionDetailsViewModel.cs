using System.Numerics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using OpenGrid.Controls;
using OpenGrid.Models;
using OpenGrid.Models.Telemetry;
using OpenGrid.Services;
using OpenGrid.Services.SessionPersist;
using SkiaSharp;

namespace OpenGrid.ViewModels;

public partial class SessionDetailsViewModel : ViewModelBase
{
    private static readonly SKColor DefaultColor = SKColors.DodgerBlue;
    private readonly Func<double, string> _gearLabeler = value => ((Gear)value).DisplayName();
    private readonly Func<double, string> _integerLabeler = value => value.ToString("N0");
    private readonly Func<double, string> _percentLabeler = value => value.ToString("P0");
    private readonly Func<double, string> _gForceLabeler = value => value.ToString("N2");

    private readonly SessionRepository _sessionRepository;
    private readonly INavigationService _navigationService;
    private SessionDetails? _details;
    
    [ObservableProperty]
    public partial string Title { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial List<SubplotDefinition> Subplots { get; private set; } = [];

    [ObservableProperty]
    public partial Lap? SelectedLap { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<Lap>? Laps { get; private set; }

    [ObservableProperty]
    public partial IReadOnlyList<LapComparisonItem> SelectedLaps { get; private set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<TrackMapSeries>? TrackData { get; private set; }

    [ObservableProperty]
    public partial bool ShowLapDetails { get; private set; }

    [ObservableProperty]
    public partial bool ShowComparisonLegend { get; private set; }

    [ObservableProperty]
    public partial bool ShowCompareWithOtherLaps { get; private set; }
    
    public bool CanGoBack => _navigationService.CanGoBack;

    public SessionDetailsViewModel(SessionRepository sessionRepository, INavigationService navigationService)
    {
        _sessionRepository = sessionRepository;
        _navigationService = navigationService;
        ShowLapDetails = true;
        ShowCompareWithOtherLaps = true;
    }

    public override void SetParameters(params object[] parameters)
    {
        if (parameters.Length > 0 && parameters[0] is SessionInfo { LapInfo.Count: > 0 } session)
        {
            SetSessionDetails(session);
        }
        else if (parameters.Length > 0 && parameters[0] is List<LapComparisonItem> comparisonItems)
        {
            SetLapComparison(comparisonItems).FireAndForgetSafe();
        }
    }

    private void SetSessionDetails(SessionInfo session)
    {
        ShowLapDetails = true;
        ShowCompareWithOtherLaps = true;
        ShowComparisonLegend = false;
        SelectedLaps = [];

        _details = _sessionRepository.LoadSessionDetails(session);
        Title = $"{_details.Info.Game.Name} - {_details.Info.Car} - {_details.Info.Track}";
        Laps = _details.Laps;
        SelectedLap = Laps.First();
    }

    private async Task SetLapComparison(IReadOnlyList<LapComparisonItem> comparisonItems)
    {
        ShowLapDetails = false;
        ShowCompareWithOtherLaps = false;
        ShowComparisonLegend = true;
        _details = null;
        Laps = null;
        SelectedLap = null;

        Title = "Lap Comparison";
        SelectedLaps = comparisonItems;
        await SetComparisonChartDataAsync();
    }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    [RelayCommand]
    private async Task DeleteLapAsync(Lap? lap)
    {
        if (_details is null || lap is null)
        {
            return;
        }

        var confirmDialog = new ConfirmDialog("Are you sure you want to delete this lap?");
        await DialogHost.Show(confirmDialog);

        if (!confirmDialog.Result)
        {
            return;
        }

        if (_details.Laps.Count == 1)
        {
            _sessionRepository.DeleteSession(_details.Info.FileName);
            _navigationService.NavigateTo<SessionsViewModel>();
            return;
        }

        _sessionRepository.DeleteLap(_details, lap.Number);

        Laps = _details.Laps;
        SelectedLap = Laps.FirstOrDefault();
    }

    [RelayCommand]
    private void CompareWithOtherLaps()
    {
        if (_details is not null)
        {
            var viewModel = new LapSelectionViewModel(_sessionRepository, _navigationService, _details);
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

        SetChartsAsync(value).FireAndForgetSafe();
        SetTrackDataAsync(value).FireAndForgetSafe();
    }

    private async Task SetTrackDataAsync(Lap lap)
    {
        if (lap.Records is null || lap.Records.All(x => x.CarPosition == Vector3.Zero))
        {
            TrackData = [];
            return;
        }

        await Task.Run(() =>
        {
            var firstPos = lap.Records.First().CarPosition;
            var xs = lap.Records.Select(r => (double)(r.CarPosition.X - firstPos.X)).ToArray();
            var ys = lap.Records.Select(r => (double)(r.CarPosition.Z - firstPos.Z)).ToArray();
            var gas = lap.Records.Select(r => r.Gas).ToArray();
            var brake = lap.Records.Select(r => r.Brake).ToArray();

            TrackData = new List<TrackMapSeries>
            {
                new()
                {
                    Name = "Track",
                    Xs = xs,
                    Ys = ys,
                    Color = SKColors.DodgerBlue,
                    GasValues = gas,
                    BrakeValues = brake,
                }
            };
        });
            

    }

    private async Task SetChartsAsync(Lap lap)
    {
        if (lap.Records is null)
        {
            return;
        }

        await Task.Run(() =>
        {
            var distanceOffset = lap.Records.First().Distance;
            var xs = lap.Records.Select(r => (double)(r.Distance - distanceOffset)).ToArray();

            var gasData = CreateChartData("Gas", xs, lap.Records.Select(r => (double)r.Gas), DefaultColor);
            var brakeData = CreateChartData("Brake", xs, lap.Records.Select(r => (double)r.Brake), DefaultColor);
            var steeringData = CreateChartData("Steering", xs, lap.Records.Select(r => (double)r.SteerAngle), DefaultColor);
            var speedData = CreateChartData("Speed", xs, lap.Records.Select(r => Math.Round(r.SpeedKmh, 1)), DefaultColor);
            var gearData = CreateChartData("Gear", xs, lap.Records.Select(r => (double)r.CurrentGear), DefaultColor, isStep: true);
            var rpmData = CreateChartData("RPM", xs, lap.Records.Select(r => (double)r.EngineRpm), DefaultColor);
            var gForceLatData = CreateChartData("Lat", xs, lap.Records.Select(r => (double)r.GForceLat), DefaultColor);

            Subplots =
            [
                new SubplotDefinition { Title = "Gas", Series = [gasData], YLabeler = _percentLabeler, YMinLimit = 0, YMaxLimit = 1.02 },
                new SubplotDefinition { Title = "Brake", Series = [brakeData], YLabeler = _percentLabeler, YMinLimit = 0, YMaxLimit = 1.02 },
                new SubplotDefinition { Title = "Steering", Series = [steeringData], YLabeler = _percentLabeler, YMinLimit = -1, YMaxLimit = 1 },
                new SubplotDefinition { Title = "Speed", Series = [speedData], YLabeler = _integerLabeler },
                new SubplotDefinition { Title = "Gear", Series = [gearData], YLabeler = _gearLabeler, YMinLimit = 0, YMaxLimit = 7 },
                new SubplotDefinition { Title = "RPM", Series = [rpmData], YLabeler = _integerLabeler, YMinLimit = 0 },
                new SubplotDefinition { Title = "G-Forces", Series = [gForceLatData], YLabeler = _gForceLabeler },
            ];
        });
    }

    private async Task SetComparisonChartDataAsync()
    {
        if (SelectedLaps.Count == 0)
        {
            return;
        }

        await Task.Run(() =>
        {
            var gasSeries = new List<ChartData>();
            var brakeSeries = new List<ChartData>();
            var steeringSeries = new List<ChartData>();
            var speedSeries = new List<ChartData>();
            var gearSeries = new List<ChartData>();
            var rpmSeries = new List<ChartData>();
            var gForceLatSeries = new List<ChartData>();

            foreach (var item in SelectedLaps)
            {
                var records = item.Lap.Records;
                if (records is null) continue;

                var distanceOffset = records.First().Distance;
                var xs = records.Select(r => (double)(r.Distance - distanceOffset)).ToArray();
                var color = item.Color;

                gasSeries.Add(CreateChartData(item.ShortName, xs, records.Select(r => (double)r.Gas), color));
                brakeSeries.Add(CreateChartData(item.ShortName, xs, records.Select(r => (double)r.Brake), color));
                steeringSeries.Add(CreateChartData(item.ShortName, xs, records.Select(r => (double)r.SteerAngle), color));
                speedSeries.Add(CreateChartData(item.ShortName, xs, records.Select(r => Math.Round(r.SpeedKmh, 1)), color));
                gearSeries.Add(CreateChartData(item.ShortName, xs, records.Select(r => (double)(int)r.CurrentGear), color, isStep: true));
                rpmSeries.Add(CreateChartData(item.ShortName, xs, records.Select(r => (double)r.EngineRpm), color));
                gForceLatSeries.Add(CreateChartData(item.ShortName, xs, records.Select(r => (double)r.GForceLat), color));
            }

            Subplots =
            [
                new SubplotDefinition { Title = "Gas", Series = [.. gasSeries], YLabeler = _percentLabeler, YMinLimit = 0, YMaxLimit = 1.02 },
                new SubplotDefinition { Title = "Brake", Series = [.. brakeSeries], YLabeler = _percentLabeler, YMinLimit = 0, YMaxLimit = 1.02 },
                new SubplotDefinition { Title = "Steering", Series = [.. steeringSeries], YLabeler = _percentLabeler, YMinLimit = -1, YMaxLimit = 1 },
                new SubplotDefinition { Title = "Speed", Series = [.. speedSeries], YLabeler = _integerLabeler },
                new SubplotDefinition { Title = "Gear", Series = [.. gearSeries], YLabeler = _gearLabeler, YMinLimit = 0, YMaxLimit = 7 },
                new SubplotDefinition { Title = "RPM", Series = [.. rpmSeries], YLabeler = _integerLabeler, YMinLimit = 0 },
                new SubplotDefinition { Title = "Lateral G-Forces", Series = [.. gForceLatSeries], YLabeler = _gForceLabeler },
            ];

            var trackSeries = new List<TrackMapSeries>();
            foreach (var item in SelectedLaps)
            {
                var records = item.Lap.Records;
                if (records is null || records.All(x => x.CarPosition == Vector3.Zero))
                {
                    continue;
                }

                var firstPos = records.First().CarPosition;
                var trackXs = records.Select(r => (double)(r.CarPosition.X - firstPos.X)).ToArray();
                var trackYs = records.Select(r => (double)(r.CarPosition.Z - firstPos.Z)).ToArray();
                trackSeries.Add(new TrackMapSeries
                {
                    Name = item.ShortName,
                    Xs = trackXs,
                    Ys = trackYs,
                    Color = item.Color
                });
            }

            TrackData = trackSeries;
        });
    }

    private static ChartData CreateChartData(string name, double[] xs, IEnumerable<double> ysEnumerable, SKColor color, float strokeThickness = 2, bool isStep = false, bool isDashed = false)
    {
        var ys = ysEnumerable.ToArray();
        return new ChartData(name, xs, ys, color, strokeThickness, isStep, isDashed);
    }
}
