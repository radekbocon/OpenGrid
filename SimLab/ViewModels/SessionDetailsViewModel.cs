using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Avalonia.Input;
using Avalonia.Xaml.Interactions.Events;
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
    private Session? Session { get; set; }

    [ObservableProperty]
    public partial Lap? SelectedLap { get; set; }

    public ObservableCollection<ChartData> Inputs { get; set; } = [];
    public ObservableCollection<ChartData> Speed { get; set; } = [];
    public ObservableCollection<ChartData> Gear { get; set; } = [];
    public ObservableCollection<ChartData> Rpm { get; set; } = [];
    
    [ObservableProperty]
    public partial double MinX { get; set; }

    [ObservableProperty]
    public partial double MaxX { get; set; }

    public override void SetParameters(params object[] parameters)
    {
        if (parameters.Length > 0 && parameters[0] is Session session)
        {
            Session = session;
            SelectedLap = session.Laps[1];
        }
    }

    partial void OnSelectedLapChanged(Lap? value)
    {
        if (value is null)
        {
            return;
        }
        
        var gasData = new ChartData("Gas", 
            new ObservableCollection<ObservablePoint>(value.Records.Select(r => new ObservablePoint(r.Distance, r.Gas))), 
            new SolidColorPaint(SKColors.Green, 3));
        var brakeData = new ChartData("Brake", 
            new ObservableCollection<ObservablePoint>(value.Records.Select(r => new ObservablePoint(r.Distance, r.Brake))), 
            new SolidColorPaint(SKColors.Red, 3));
        var steeringData = new ChartData("Steering", 
            new ObservableCollection<ObservablePoint>(value.Records.Select(r => new ObservablePoint(r.Distance, Math.Abs(Math.Round(r.SteerAngle, 2))))), 
            new SolidColorPaint(SKColors.Gray, 3));
        var speedData = new ChartData("Speed", 
            new ObservableCollection<ObservablePoint>(value.Records.Select(r => new ObservablePoint(r.Distance, Math.Round(r.SpeedKmh, 1)))), 
            new SolidColorPaint(SKColors.DodgerBlue, 3));
        var gearData = new ChartData("Gear", 
            new ObservableCollection<ObservablePoint>(value.Records.Select(r => new ObservablePoint(r.Distance, (int)r.CurrentGear))), 
            new SolidColorPaint(SKColors.DodgerBlue, 3));
        var rpmData = new ChartData("RPM", 
            new ObservableCollection<ObservablePoint>(value.Records.Select(r => new ObservablePoint(r.Distance, r.EngineRpm))), 
            new SolidColorPaint(SKColors.DodgerBlue, 3));
        
        MinX = value.Records.Min(r => r.Distance);
        MaxX = value.Records.Max(r => r.Distance);

        Inputs = [gasData, brakeData, steeringData];
        Speed = [speedData];
        Gear = [gearData];
        Rpm = [rpmData];
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

public class ChartData(string name, ObservableCollection<ObservablePoint> points, Paint stroke)
{
    public string Name { get; set; } = name;
    
    public ObservableCollection<ObservablePoint> Values { get; set; } = points;

    public Paint Stroke { get; set; } = stroke;
}