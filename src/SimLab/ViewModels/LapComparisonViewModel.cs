using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore.Defaults;
using LiveChartsCore.Painting;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using SimLab.Models;
using SimLab.Services;
using SkiaSharp;

namespace SimLab.ViewModels;

public partial class LapComparisonViewModel : LapChartViewModelBase
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    public partial ObservableCollection<LapComparisonItem> SelectedLaps { get; set; } = [];

    private const float StrokeThickness = 2;
    private static readonly DashEffect DashEffect = new DashEffect([3 * StrokeThickness, 2 * StrokeThickness]);

    public LapComparisonViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    public override void SetParameters(params object[] parameters)
    {
        if (parameters.Length > 0 && parameters[0] is List<LapComparisonItem> comparisonItems)
        {
            SelectedLaps = new ObservableCollection<LapComparisonItem>(comparisonItems);
            GenerateChartData();
        }
    }

    private static Paint CreateDashedPaint(SKColor color)
    {
        return new SolidColorPaint(color, StrokeThickness)
        {
            PathEffect = DashEffect,
            StrokeCap = SKStrokeCap.Round
        };
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
            
            var lapMinX = item.Lap.Records.Min(r => r.Distance - distanceOffset);
            var lapMaxX = item.Lap.Records.Max(r => r.Distance - distanceOffset);
            
            globalMinX = Math.Min(globalMinX, lapMinX);
            globalMaxX = Math.Max(globalMaxX, lapMaxX);

            inputsSeries.Add(new ChartData($"{item.ShortName} (Gas)",
                item.Lap.Records.Select(r => new ObservablePoint(r.Distance - distanceOffset, r.Gas)),
                item.Paint));

            inputsSeries.Add(new ChartData($"{item.ShortName} (Brake)",
                item.Lap.Records.Select(r => new ObservablePoint(r.Distance - distanceOffset, r.Brake)),
                CreateDashedPaint(item.Color)));

            steeringSeries.Add(new ChartData($"{item.ShortName}",
                item.Lap.Records.Select(r => new ObservablePoint(r.Distance - distanceOffset, r.SteerAngle)),
                item.Paint));

            speedSeries.Add(new ChartData($"{item.ShortName}",
                item.Lap.Records.Select(r => new ObservablePoint(r.Distance - distanceOffset, Math.Round(r.SpeedKmh, 1))),
                item.Paint));

            gearSeries.Add(new ChartData($"{item.ShortName}",
                item.Lap.Records.Select(r => new ObservablePoint(r.Distance - distanceOffset, (int)r.CurrentGear)),
                item.Paint));

            rpmSeries.Add(new ChartData($"{item.ShortName}",
                item.Lap.Records.Select(r => new ObservablePoint(r.Distance - distanceOffset, r.EngineRpm)),
                item.Paint));
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

        ResampleAllCharts();
    }
}
