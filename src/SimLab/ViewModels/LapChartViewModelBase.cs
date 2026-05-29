using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using SimLab.Controls;
using SimLab.Models;
using SimLab.Models.Telemetry;
using SkiaSharp;

namespace SimLab.ViewModels;

public abstract partial class LapChartViewModelBase : ViewModelBase, ILapChartViewModel
{
    [ObservableProperty]
    public partial List<SubplotDefinition> Subplots { get; set; } = [];

    public Func<double, string> GearLabeler { get; set; } = value => Enum.GetName(typeof(Gear), (int)value) ?? "";
    public Func<double, string> DoubleLabeler { get; set; } = value => value.ToString("N2");
    public Func<double, string> IntegerLabeler { get; set; } = value => value.ToString("N0");

    protected static ChartData CreateChartData(string name, double[] xs, IEnumerable<double> ysEnumerable, SKColor color, float strokeThickness = 2, bool isStep = false, bool isDashed = false)
    {
        var ys = ysEnumerable.ToArray();
        return new ChartData(name, xs, ys, color, strokeThickness, isStep, isDashed);
    }
}
