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

    public ObservableCollection<ChartData> Inputs { get; set; } = [];
    public ObservableCollection<ChartData> Speed { get; set; } = [];
    public ObservableCollection<ChartData> Gear { get; set; } = [];
    public ObservableCollection<ChartData> Rpm { get; set; } = [];
    
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
            SelectedLap = Laps.FirstOrDefault();
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
        var speedData = new ChartData("Speed", 
            value.Records.Select(r => new ObservablePoint(r.Distance, Math.Round(r.SpeedKmh, 1))), 
            new SolidColorPaint(SKColors.DodgerBlue,2));
        var gearData = new ChartData("Gear", 
            value.Records.Select(r => new ObservablePoint(r.Distance, (int)r.CurrentGear)), 
            new SolidColorPaint(SKColors.DodgerBlue, 2));
        var rpmData = new ChartData("RPM", 
            value.Records.Select(r => new ObservablePoint(r.Distance, r.EngineRpm)), 
            new SolidColorPaint(SKColors.DodgerBlue, 2));
        
        MinX = value.Records.Min(r => r.Distance);
        MaxX = value.Records.Max(r => r.Distance);

        Inputs = [gasData, brakeData];
        Speed = [speedData];
        Gear = [gearData];
        Rpm = [rpmData];
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
    }
}

public class ChartData
{
    public string Name { get; set; }

    public ObservableCollection<ObservablePoint> Values { get; set; }

    public Paint Stroke { get; set; }

    public ChartData(string name, IEnumerable<ObservablePoint> values, Paint stroke)
    {
        Name = name;
        Values = new ObservableCollection<ObservablePoint>(values);
        Stroke = stroke;
    }
}