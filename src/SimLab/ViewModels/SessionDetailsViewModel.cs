using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore.Defaults;
using LiveChartsCore.Generators;
using LiveChartsCore.Painting;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SimLab.Models;
using SkiaSharp;

namespace SimLab.ViewModels;

public partial class SessionDetailsViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial Lap? SelectedLap { get; set; }
    
    public ObservableCollection<Lap>? Laps { get; set; } 

    [ObservableProperty]
    public partial ObservableCollection<ChartData> Inputs { get; set; } = [];
    [ObservableProperty]
    public partial ObservableCollection<ChartData> Speed { get; set; } = [];
    [ObservableProperty]
    public partial ObservableCollection<ChartData> Gear { get; set; } = [];
    [ObservableProperty]
    public partial ObservableCollection<ChartData> Rpm { get; set; } = [];
    
    public double DataMinX { get; private set; }
    public double DataMaxX { get; private set; }

    [ObservableProperty]
    public partial double MinX { get; set; }

    [ObservableProperty]
    public partial double MaxX { get; set; }
    
    public Func<double, string> GearLabeler { get; set; } =
        value => Enum.GetName(typeof(Gear), (int)value) ?? "";

    [ObservableProperty]
    public partial string? LapTime { get; set; }

    public override void SetParameters(params object[] parameters)
    {
        if (parameters.Length > 0 && parameters[0] is Session { Laps.Count: > 0 } session)
        {
            Laps = new ObservableCollection<Lap>(session.Laps);
            SelectedLap = Laps.First();
        }
    }

    partial void OnSelectedLapChanged(Lap? value)
    {
        if (value is null)
        {
            return;
        }
        
        var gasData = new ChartData("Gas", 
            value.Records.Select(r => new ObservablePoint(r.Distance, r.Gas)), 
            new SolidColorPaint(SKColors.Green, 2));
        var brakeData = new ChartData("Brake", 
            value.Records.Select(r => new ObservablePoint(r.Distance, r.Brake)), 
            new SolidColorPaint(SKColors.Red, 2));
        var steeringData = new ChartData("Steering", 
            value.Records.Select(r => new ObservablePoint(r.Distance, Math.Abs(r.SteerAngle))), 
            new SolidColorPaint(SKColors.Gray, 2));
        var speedData = new ChartData("Speed", 
            value.Records.Select(r => new ObservablePoint(r.Distance, Math.Round(r.SpeedKmh, 1))), 
            new SolidColorPaint(SKColors.DodgerBlue,2));
        var gearData = new ChartData("Gear", 
            value.Records.Select(r => new ObservablePoint(r.Distance, (int)r.CurrentGear)), 
            new SolidColorPaint(SKColors.DodgerBlue, 2));
        var rpmData = new ChartData("RPM", 
            value.Records.Select(r => new ObservablePoint(r.Distance, r.EngineRpm)), 
            new SolidColorPaint(SKColors.DodgerBlue, 2));
        
        DataMinX = value.Records.Min(r => r.Distance);
        DataMaxX = value.Records.Max(r => r.Distance);
        MinX = DataMinX;
        MaxX = DataMaxX;

        Inputs = [gasData, brakeData, steeringData];
        Speed = [speedData];
        Gear = [gearData];
        Rpm = [rpmData];
        
        ResampleAllCharts();
        LapTime = $@"Time: {SelectedLap?.Time:mm\:ss\.fff}";
    }

    [RelayCommand]
    private void ChangeZoom(CommandParameters<object, PropertyChangedEventArgs> e)
    {
        if (e.Parameter1 is not Axis axis)
        {
            return;
        }
        MaxX = axis.MaxLimit ?? MaxX;
        MinX = axis.MinLimit ?? MinX;
        ResampleAllCharts();
    }
    
    private void ResampleAllCharts()
    {
        // TODO: set actual width
        const int widthPixels = 500;
        
        foreach (var chartData in Rpm)
        {
            chartData.Resample(widthPixels, MaxX, MinX);
        }
        foreach (var chartData in Speed)
        {
            chartData.Resample(widthPixels, MaxX, MinX);
        }
        foreach (var chartData in Gear)
        {
            chartData.Resample(widthPixels, MaxX, MinX);
        }
        foreach (var chartData in Inputs)
        {
            chartData.Resample(widthPixels, MaxX, MinX);
        }
    }
}

public class ChartData
{
    private readonly List<ObservablePoint> _fullData;
    
    public string Name { get; set; }

    public ObservableCollection<ObservablePoint> Points { get; set; }

    public Paint Stroke { get; set; }

    public ChartData(string name, IEnumerable<ObservablePoint> values, Paint stroke)
    {
        _fullData = values.ToList();
        
        Name = name;
        Points = new ObservableCollection<ObservablePoint>(_fullData);
        Stroke = stroke;
    }

    public void Resample(int widthPixels, double maxX, double minX)
    {
        const int pixelsPerPoint = 2;

        var maxPoints = widthPixels / pixelsPerPoint;

        // Get only visible points
        var visible = _fullData
            .Where(p => p.X >= minX && p.X <= maxX)
            .ToList();

        if (visible.Count <= maxPoints)
        {
            // No need to resample
            Points.Clear();
            foreach (var p in visible)
                Points.Add(p);

            return;
        }

        // Calculate step
        var step = (double)visible.Count / maxPoints;

        var resampled = new List<ObservablePoint>(maxPoints);

        for (var i = 0; i < maxPoints; i++)
        {
            var index = (int)(i * step);
            if (index >= visible.Count)
            {
                index = visible.Count - 1;
            }

            resampled.Add(visible[index]);
        }

        Points.Clear();
        foreach (var p in resampled)
        {
            Points.Add(p);
        }
    }
}