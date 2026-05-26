using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using SimLab.Models;
using SimLab.Services;

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

        var inputsSeries = new List<ChartData>();
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
                var (dsXs, dsGasYs) = DataDownsampler.Decimate(xs, gasYs);
                inputsSeries.Add(new ChartData($"{item.ShortName} (Gas)", dsXs, dsGasYs, color));

                var brakeYs = item.Lap.Records.Select(r => (double)r.Brake).ToArray();
                var (dsXs2, dsBrakeYs) = DataDownsampler.Decimate(xs, brakeYs);
                inputsSeries.Add(new ChartData($"{item.ShortName} (Brake)", dsXs2, dsBrakeYs, color, isDashed: true));

                var steerYs = item.Lap.Records.Select(r => (double)r.SteerAngle).ToArray();
                var (dsXs3, dsSteerYs) = DataDownsampler.Decimate(xs, steerYs);
                steeringSeries.Add(new ChartData($"{item.ShortName}", dsXs3, dsSteerYs, color));

                var speedYs = item.Lap.Records.Select(r => Math.Round(r.SpeedKmh, 1)).ToArray();
                var (dsXs4, dsSpeedYs) = DataDownsampler.Decimate(xs, speedYs);
                speedSeries.Add(new ChartData($"{item.ShortName}", dsXs4, dsSpeedYs, color));

                var gearYs = item.Lap.Records.Select(r => (double)(int)r.CurrentGear).ToArray();
                var (dsXs5, dsGearYs) = DataDownsampler.Decimate(xs, gearYs);
                gearSeries.Add(new ChartData($"{item.ShortName}", dsXs5, dsGearYs, color, isStep: true));

                var rpmYs = item.Lap.Records.Select(r => (double)r.EngineRpm).ToArray();
                var (dsXs6, dsRpmYs) = DataDownsampler.Decimate(xs, rpmYs);
                rpmSeries.Add(new ChartData($"{item.ShortName}", dsXs6, dsRpmYs, color));
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
        });
    }
}