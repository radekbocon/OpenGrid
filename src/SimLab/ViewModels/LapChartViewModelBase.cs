using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using SimLab.Models;
using SkiaSharp;

namespace SimLab.ViewModels;

public abstract partial class LapChartViewModelBase : ViewModelBase, ILapChartViewModel
{
    [ObservableProperty]
    public partial ObservableCollection<ChartData> Gas { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<ChartData> Brake { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<ChartData> Steering { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<ChartData> Speed { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<ChartData> Gear { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<ChartData> Rpm { get; set; } = [];

    public double DataMinX { get; protected set; }
    public double DataMaxX { get; protected set; }

    [ObservableProperty]
    public partial double MinX { get; set; }

    [ObservableProperty]
    public partial double MaxX { get; set; }

    public Func<double, string> GearLabeler { get; set; } = value => Enum.GetName(typeof(Gear), (int)value) ?? "";
    public Func<double, string> DoubleLabeler { get; set; } = value => value.ToString("N2");
    public Func<double, string> IntegerLabeler { get; set; } = value => value.ToString("N0");
    
    protected static ChartData CreateDownsampledChartData(string name, double[] xs, IEnumerable<double> ysEnumerable, SKColor color, float strokeThickness = 2, bool isStep = false, bool isDashed = false)
    {
        var ys = ysEnumerable.ToArray();
        var (downsampledXs, downsampledYs) = DataDownsampler.Decimate(xs, ys);
        return new ChartData(name, downsampledXs, downsampledYs, color, strokeThickness, isStep, isDashed);
    }
}
