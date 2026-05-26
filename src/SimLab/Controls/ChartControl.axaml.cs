using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ScottPlot;
using ScottPlot.Avalonia;
using SimLab.ViewModels;

namespace SimLab.Controls;

public partial class ChartControl : UserControl
{
    public AvaPlot Chart => ChartElement;

    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<ChartControl, string>(nameof(Title));

    public static readonly StyledProperty<IEnumerable<ChartData>> SeriesSourceProperty =
        AvaloniaProperty.Register<ChartControl, IEnumerable<ChartData>>(nameof(SeriesSource), []);

    public static readonly StyledProperty<Func<double, string>> YLabelerProperty =
        AvaloniaProperty.Register<ChartControl, Func<double, string>>(nameof(YLabeler));

    public static readonly StyledProperty<double?> YMaxLimitProperty =
        AvaloniaProperty.Register<ChartControl, double?>(nameof(YMaxLimit));

    public static readonly StyledProperty<double?> YMinLimitProperty =
        AvaloniaProperty.Register<ChartControl, double?>(nameof(YMinLimit));

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public IEnumerable<ChartData> SeriesSource
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

    public ChartControl()
    {
        InitializeComponent();
        ChartElement.UserInputProcessor.IsEnabled = false;

        // give the plot a dark background with light text
        ChartElement.Plot.FigureBackground.Color = new Color("#1c1c1e");
        ChartElement.Plot.Axes.Color(new Color("#888888"));
        
        // shade regions between major grid lines
        ChartElement.Plot.Grid.XAxisStyle.FillColor1 = new Color("#888888").WithAlpha(10);
        ChartElement.Plot.Grid.YAxisStyle.FillColor1 = new Color("#888888").WithAlpha(10);

        // set grid line colors
        ChartElement.Plot.Grid.XAxisStyle.MajorLineStyle.Color = Colors.White.WithAlpha(15);
        ChartElement.Plot.Grid.YAxisStyle.MajorLineStyle.Color = Colors.White.WithAlpha(15);
        ChartElement.Plot.Grid.XAxisStyle.MinorLineStyle.Color = Colors.White.WithAlpha(5);
        ChartElement.Plot.Grid.YAxisStyle.MinorLineStyle.Color = Colors.White.WithAlpha(5);

        // enable minor grid lines by defining a positive width
        ChartElement.Plot.Grid.XAxisStyle.MinorLineStyle.Width = 1;
        ChartElement.Plot.Grid.YAxisStyle.MinorLineStyle.Width = 1;
        UpdatePlot();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SeriesSourceProperty ||
            change.Property == YMaxLimitProperty ||
            change.Property == YMinLimitProperty ||
            change.Property == YLabelerProperty)
        {
            Dispatcher.UIThread.Post(UpdatePlot, DispatcherPriority.Background);
        }
    }

    private void UpdatePlot()
    {
        var plot = ChartElement.Plot;
        plot.Clear();

        var series = SeriesSource;
        var chartDataList = series.ToList();
        if (chartDataList.Count == 0)
        {
            ChartElement.Refresh();
            return;
        }

        foreach (var data in chartDataList)
        {
            var signalXy = plot.Add.SignalXY(data.Xs, data.Ys);
            signalXy.Color = new Color(data.StrokeColor.Red, data.StrokeColor.Green, data.StrokeColor.Blue,
                data.StrokeColor.Alpha);
            signalXy.LineWidth = data.StrokeThickness;
            signalXy.MarkerSize = 0;
            signalXy.LegendText = data.Name;

            if (data.IsStep)
            {
                signalXy.ConnectStyle = ConnectStyle.StepHorizontal;
            }

            if (data.IsDashed)
            {
                signalXy.LineStyle.Pattern = LinePattern.Dashed;
            }
        }

        plot.Legend.IsVisible = false;

        var yLabeler = YLabeler;
        var tickGen = new ScottPlot.TickGenerators.NumericAutomatic
        {
            LabelFormatter = yLabeler
        };
        plot.Axes.Left.TickGenerator = tickGen;


        if (YMinLimit.HasValue || YMaxLimit.HasValue)
        {
            var yMin = YMinLimit ?? plot.Axes.GetLimits().Bottom;
            var yMax = YMaxLimit ?? plot.Axes.GetLimits().Top;
            plot.Axes.SetLimitsY(yMin, yMax);
        }

        ChartElement.Refresh();
    }
}