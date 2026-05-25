using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using SimLab.Models;
using SimLab.Services;

namespace SimLab.ViewModels;

public partial class LapComparisonViewModel : LapChartViewModelBase
{
    [ObservableProperty]
    public partial ObservableCollection<LapComparisonItem> SelectedLaps { get; set; } = [];

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

        var inputsSeries = new List<ChartData>();
        var steeringSeries = new List<ChartData>();
        var speedSeries = new List<ChartData>();
        var gearSeries = new List<ChartData>();
        var rpmSeries = new List<ChartData>();

        double globalMinX = double.MaxValue;
        double globalMaxX = double.MinValue;

        foreach (var item in SelectedLaps)
        {
            var distanceOffset = item.Lap.Records.First().Distance;

            var lapMinX = item.Lap.Records.Min(r => (double)(r.Distance - distanceOffset));
            var lapMaxX = item.Lap.Records.Max(r => (double)(r.Distance - distanceOffset));

            globalMinX = Math.Min(globalMinX, lapMinX);
            globalMaxX = Math.Max(globalMaxX, lapMaxX);

            var xs = item.Lap.Records.Select(r => (double)(r.Distance - distanceOffset)).ToArray();
            var gasYs = item.Lap.Records.Select(r => (double)r.Gas);
            inputsSeries.Add(new ChartData($"{item.ShortName} (Gas)", xs, gasYs, item.Color));

            var brakeYs = item.Lap.Records.Select(r => (double)r.Brake);
            inputsSeries.Add(new ChartData($"{item.ShortName} (Brake)", xs, brakeYs, item.Color, isDashed: true));

            var steerYs = item.Lap.Records.Select(r => (double)r.SteerAngle);
            steeringSeries.Add(new ChartData($"{item.ShortName}", xs, steerYs, item.Color));

            var speedYs = item.Lap.Records.Select(r => Math.Round(r.SpeedKmh, 1));
            speedSeries.Add(new ChartData($"{item.ShortName}", xs, speedYs, item.Color));

            var gearYs = item.Lap.Records.Select(r => (double)(int)r.CurrentGear);
            gearSeries.Add(new ChartData($"{item.ShortName}", xs, gearYs, item.Color, isStep: true));

            var rpmYs = item.Lap.Records.Select(r => (double)r.EngineRpm);
            rpmSeries.Add(new ChartData($"{item.ShortName}", xs, rpmYs, item.Color));
        }

        DataMinX = globalMinX;
        DataMaxX = globalMaxX;
        MinX = DataMinX;
        MaxX = DataMaxX;

        Inputs = new ObservableCollection<ChartData>(inputsSeries);
        Steering = new ObservableCollection<ChartData>(steeringSeries);
        Speed = new ObservableCollection<ChartData>(speedSeries);
        Gear = new ObservableCollection<ChartData>(gearSeries);
        Rpm = new ObservableCollection<ChartData>(rpmSeries);
    }
}
