using System;
using System.Collections;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using LiveChartsCore.Drawing;
using LiveChartsCore.Painting;
using LiveChartsCore.SkiaSharpView.Avalonia;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace SimLab.Controls;

public partial class ChartControl : UserControl
{
    public CartesianChart Chart => ChartElement;
    
    public static readonly StyledProperty<DataTemplate?> SeriesTemplateProperty =
        AvaloniaProperty.Register<ChartControl, DataTemplate?>(nameof(SeriesTemplate));

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

    public static readonly StyledProperty<IBrush> CrosshairLabelsBackgroundProperty =
        AvaloniaProperty.Register<ChartControl, IBrush>(nameof(CrosshairLabelsBackground),
            new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)));

    public static readonly StyledProperty<IBrush> CrosshairLabelsPaintProperty =
        AvaloniaProperty.Register<ChartControl, IBrush>(nameof(CrosshairLabelsPaint),
            new SolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xEE)));

    public static readonly StyledProperty<IBrush> CrosshairPaintProperty =
        AvaloniaProperty.Register<ChartControl, IBrush>(nameof(CrosshairPaint),
            new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99)));

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

    public DataTemplate? SeriesTemplate
    {
        get => GetValue(SeriesTemplateProperty);
        set => SetValue(SeriesTemplateProperty, value);
    }

    public IBrush CrosshairLabelsBackground
    {
        get => GetValue(CrosshairLabelsBackgroundProperty);
        set => SetValue(CrosshairLabelsBackgroundProperty, value);
    }

    public IBrush CrosshairLabelsPaint
    {
        get => GetValue(CrosshairLabelsPaintProperty);
        set => SetValue(CrosshairLabelsPaintProperty, value);
    }

    public IBrush CrosshairPaint
    {
        get => GetValue(CrosshairPaintProperty);
        set => SetValue(CrosshairPaintProperty, value);
    }

    static ChartControl()
    {
        CrosshairLabelsBackgroundProperty.Changed.AddClassHandler<ChartControl>(
            (control, e) => control.OnCrosshairStyleChanged());
        CrosshairLabelsPaintProperty.Changed.AddClassHandler<ChartControl>(
            (control, e) => control.OnCrosshairStyleChanged());
        CrosshairPaintProperty.Changed.AddClassHandler<ChartControl>(
            (control, e) => control.OnCrosshairStyleChanged());
    }

    public ChartControl()
    {
        InitializeComponent();
        UpdateCrosshairStyles();
    }

    private void OnCrosshairStyleChanged()
    {
        UpdateCrosshairStyles();
    }

    private void UpdateCrosshairStyles()
    {
        if (ChartElement.XAxes.FirstOrDefault() is not XamlAxis xAxis) return;
        if (ChartElement.YAxes.FirstOrDefault() is not XamlAxis yAxis) return;

        xAxis.CrosshairLabelsBackground = BrushToLvcColor(CrosshairLabelsBackground);
        yAxis.CrosshairLabelsBackground = BrushToLvcColor(CrosshairLabelsBackground);

        xAxis.CrosshairLabelsPaint = BrushToPaint(CrosshairLabelsPaint);
        yAxis.CrosshairLabelsPaint = BrushToPaint(CrosshairLabelsPaint);

        xAxis.CrosshairPaint = BrushToPaint(CrosshairPaint);
        yAxis.CrosshairPaint = BrushToPaint(CrosshairPaint);
    }

    private static Paint? BrushToPaint(IBrush? brush)
    {
        if (brush is ISolidColorBrush solid)
        {
            var c = solid.Color;
            return new SolidColorPaint(new SKColor(c.R, c.G, c.B, c.A));
        }

        return null;
    }
    
    private static LvcColor? BrushToLvcColor(IBrush? brush)
    {
        if (brush is ISolidColorBrush solid)
        {
            var c = solid.Color;
            return new LvcColor(c.R, c.G, c.B, c.A);
        }

        return null;
    }
}
