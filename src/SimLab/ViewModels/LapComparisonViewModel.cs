using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SimLab.ViewModels;

public partial class LapComparisonViewModel : LapChartViewModelBase
{
    [ObservableProperty] public partial ObservableCollection<LapComparisonItem> SelectedLaps { get; set; } = [];

    public override void SetParameters(params object[] parameters)
    {
        if (parameters.Length > 0 && parameters[0] is List<LapComparisonItem> comparisonItems)
        {
            SelectedLaps = new ObservableCollection<LapComparisonItem>(comparisonItems);
            GenerateChartData();
        }
    }

    private void GenerateChartData()
    {
        if (SelectedLaps.Count == 0) return;

        var gasSeries = new List<ChartData>();
        var brakeSeries = new List<ChartData>();
        var steeringSeries = new List<ChartData>();
        var speedSeries = new List<ChartData>();
        var gearSeries = new List<ChartData>();
        var rpmSeries = new List<ChartData>();

        var globalMinX = double.MaxValue;
        var globalMaxX = double.MinValue;

        Task.Run(() =>
        {
            foreach (var item in SelectedLaps)
            {
                var distanceOffset = item.Lap.Records.First().Distance;

                var lapMinX = item.Lap.Records.Min(r => (double)(r.Distance - distanceOffset));
                var lapMaxX = item.Lap.Records.Max(r => (double)(r.Distance - distanceOffset));

                globalMinX = Math.Min(globalMinX, lapMinX);
                globalMaxX = Math.Max(globalMaxX, lapMaxX);

                var xs = item.Lap.Records.Select(r => (double)(r.Distance - distanceOffset)).ToArray();
                var color = item.Color;

                var gasYs = item.Lap.Records.Select(r => (double)r.Gas).ToArray();
                gasSeries.Add(CreateDownsampledChartData($"{item.ShortName}", xs, gasYs, color));

                var brakeYs = item.Lap.Records.Select(r => (double)r.Brake).ToArray();
                brakeSeries.Add(CreateDownsampledChartData($"{item.ShortName}", xs, brakeYs, color));

                var steerYs = item.Lap.Records.Select(r => (double)r.SteerAngle).ToArray();
                steeringSeries.Add(CreateDownsampledChartData($"{item.ShortName}", xs, steerYs, color));

                var speedYs = item.Lap.Records.Select(r => Math.Round(r.SpeedKmh, 1)).ToArray();
                speedSeries.Add(CreateDownsampledChartData($"{item.ShortName}", xs, speedYs, color));

                var gearYs = item.Lap.Records.Select(r => (double)(int)r.CurrentGear).ToArray();
                gearSeries.Add(CreateDownsampledChartData($"{item.ShortName}", xs, gearYs, color, isStep: true));

                var rpmYs = item.Lap.Records.Select(r => (double)r.EngineRpm).ToArray();
                rpmSeries.Add(CreateDownsampledChartData($"{item.ShortName}", xs, rpmYs, color));
            }

            DataMinX = globalMinX;
            DataMaxX = globalMaxX;
            MinX = DataMinX;
            MaxX = DataMaxX;

            Gas = new ObservableCollection<ChartData>(gasSeries);
            Brake = new ObservableCollection<ChartData>(brakeSeries);
            Steering = new ObservableCollection<ChartData>(steeringSeries);
            Speed = new ObservableCollection<ChartData>(speedSeries);
            Gear = new ObservableCollection<ChartData>(gearSeries);
            Rpm = new ObservableCollection<ChartData>(rpmSeries);
        });
    }
}