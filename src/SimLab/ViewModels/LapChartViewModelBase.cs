using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore.Generators;
using LiveChartsCore.SkiaSharpView;
using SimLab.Models;

namespace SimLab.ViewModels;

public abstract partial class LapChartViewModelBase : ViewModelBase, ILapChartViewModel
{
    [ObservableProperty]
    public partial ObservableCollection<ChartData> Inputs { get; set; } = [];

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

    protected const int DefaultWidthPixels = 1000;

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

    protected virtual void ResampleAllCharts()
    {
        foreach (var chartData in Rpm) chartData.Resample(DefaultWidthPixels, MaxX, MinX);
        foreach (var chartData in Speed) chartData.Resample(DefaultWidthPixels, MaxX, MinX);
        foreach (var chartData in Gear) chartData.Resample(DefaultWidthPixels, MaxX, MinX);
        foreach (var chartData in Inputs) chartData.Resample(DefaultWidthPixels, MaxX, MinX);
        foreach (var chartData in Steering) chartData.Resample(DefaultWidthPixels, MaxX, MinX);
    }
}
