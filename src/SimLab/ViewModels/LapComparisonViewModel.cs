using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using SimLab.Controls;
using SimLab.Models;

namespace SimLab.ViewModels;

public partial class LapComparisonViewModel : LapChartViewModelBase
{
    [ObservableProperty]
    public partial IReadOnlyList<LapComparisonItem> SelectedLaps { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<TrackMapSeries>? TrackData { get; set; }

    public override void SetParameters(params object[] parameters)
    {
        if (parameters.Length > 0 && parameters[0] is List<LapComparisonItem> comparisonItems)
        {
            SelectedLaps = comparisonItems;
            GenerateChartData();
        }
    }

    private void GenerateChartData()
    {
        if (SelectedLaps.Count == 0) return;

        Task.Run(() =>
        {
            var gasSeries = new List<ChartData>();
            var brakeSeries = new List<ChartData>();
            var steeringSeries = new List<ChartData>();
            var speedSeries = new List<ChartData>();
            var gearSeries = new List<ChartData>();
            var rpmSeries = new List<ChartData>();

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
            }

            Subplots =
            [
                new SubplotDefinition { Title = "Gas", Series = [.. gasSeries], YLabeler = DoubleLabeler, YMinLimit = 0, YMaxLimit = 1.02 },
                new SubplotDefinition { Title = "Brake", Series = [.. brakeSeries], YLabeler = DoubleLabeler, YMinLimit = 0, YMaxLimit = 1.02 },
                new SubplotDefinition { Title = "Steering", Series = [.. steeringSeries], YLabeler = DoubleLabeler, YMinLimit = -1, YMaxLimit = 1 },
                new SubplotDefinition { Title = "Speed", Series = [.. speedSeries], YLabeler = IntegerLabeler },
                new SubplotDefinition { Title = "Gear", Series = [.. gearSeries], YLabeler = GearLabeler, YMinLimit = 0, YMaxLimit = 7 },
                new SubplotDefinition { Title = "RPM", Series = [.. rpmSeries], YLabeler = IntegerLabeler, YMinLimit = 0 },
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
}
