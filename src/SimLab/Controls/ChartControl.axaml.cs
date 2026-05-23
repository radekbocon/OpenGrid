using System;
using System.Collections;
using Avalonia;
using Avalonia.Controls;
using LiveChartsCore.SkiaSharpView.Avalonia;

namespace SimLab.Controls;

public partial class ChartControl : UserControl
{
    public CartesianChart Chart => ChartElement;

    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<ChartControl, string>(nameof(Title));

    public static readonly StyledProperty<IEnumerable> SeriesSourceProperty =
        AvaloniaProperty.Register<ChartControl, IEnumerable>(nameof(SeriesSource));

    public static readonly StyledProperty<Func<double, string>> YLabelerProperty =
        AvaloniaProperty.Register<ChartControl, Func<double, string>>(nameof(YLabeler));

    public static readonly StyledProperty<double?> YMaxLimitProperty =
        AvaloniaProperty.Register<ChartControl, double?>(nameof(YMaxLimit));

    public static readonly StyledProperty<double?> YMinLimitProperty =
        AvaloniaProperty.Register<ChartControl, double?>(nameof(YMinLimit));

    public static readonly StyledProperty<double?> YMinStepProperty =
        AvaloniaProperty.Register<ChartControl, double?>(nameof(YMinStep));

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public IEnumerable SeriesSource
    {
        get => GetValue(SeriesSourceProperty);
        set => SetValue(SeriesSourceProperty, value);
    }

    public Func<double, string> YLabeler
    {
        get => GetValue(YLabelerProperty);
        set => SetValue(YLabelerProperty, value);
    }

    public double? YMaxLimit
    {
        get => GetValue(YMaxLimitProperty);
        set => SetValue(YMaxLimitProperty, value);
    }

    public double? YMinLimit
    {
        get => GetValue(YMinLimitProperty);
        set => SetValue(YMinLimitProperty, value);
    }

    public double? YMinStep
    {
        get => GetValue(YMinStepProperty);
        set => SetValue(YMinStepProperty, value);
    }

    public ChartControl()
    {
        InitializeComponent();
    }
}
