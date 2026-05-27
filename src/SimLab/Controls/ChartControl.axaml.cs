using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ScottPlot;
using ScottPlot.Interactivity;
using ScottPlot.Interactivity.UserActionResponses;
using ScottPlot.Interactivity.UserActions;

namespace SimLab.Controls;

public partial class ChartControl : UserControl
{
    private const int SubplotHeight = 250;
    
    private double _minX;
    private double _maxX;

    public static readonly StyledProperty<IEnumerable<SubplotDefinition>> SubplotsProperty =
        AvaloniaProperty.Register<ChartControl, IEnumerable<SubplotDefinition>>(nameof(Subplots), []);

    public IEnumerable<SubplotDefinition> Subplots
    {
        get => GetValue(SubplotsProperty);
        set => SetValue(SubplotsProperty, value);
    }

    public ChartControl()
    {
        InitializeComponent();
        IsVisible = false;
        ChartElement.AddHandler(PointerWheelChangedEvent, PointerWheelHandler, RoutingStrategies.Tunnel);
        ChartElement.UserInputProcessor.RemoveAll<IUserActionResponse>();
        ChartElement.UserInputProcessor.UserActionResponses.Add(new MouseDragPan(StandardMouseButtons.Left) { LockY = true });
        ChartElement.UserInputProcessor.UserActionResponses.Add(new XOnlyMouseWheelZoom(this));
    }

    private void PointerWheelHandler(object? sender, PointerWheelEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            return;
        }

        var scrollViewer = ChartElement.FindAncestorOfType<ScrollViewer>();
        var up = e.Delta.Y > 0;

        if (up)
        {
            scrollViewer?.LineUp();
        }
        else
        {
            scrollViewer?.LineDown();
        }
        e.Handled = true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SubplotsProperty)
        {
            Dispatcher.UIThread.Post(UpdatePlots, DispatcherPriority.Background);
        }
    }

    private void UpdatePlots()
    {
        var definitions = Subplots.ToList();

        ChartElement.MinHeight = definitions.Count * SubplotHeight;

        if (definitions.Count == 0)
        {
            ChartElement.Reset();
            return;
        }

        var multiplot = new Multiplot
        {
            Layout = new ScottPlot.MultiplotLayouts.Grid(definitions.Count, 1)
        };

        while (multiplot.Subplots.Count > 0)
        {
            multiplot.Subplots.RemoveAt(0);
        }
        var maxX = 0.0;
        var minX = 0.0;
        var plots = new List<Plot>();

        foreach (var def in definitions)
        {
            var plot = new Plot();
            ApplySubplotStyle(plot);
            plot.Title(def.Title);

            foreach (var data in def.Series)
            {
                var signalXy = plot.Add.SignalXY(data.Xs, data.Ys);
                maxX = Math.Max(maxX, data.Xs.Max());
                minX = Math.Min(minX, data.Xs.Min());
                signalXy.Color = new Color(data.StrokeColor.Red, data.StrokeColor.Green, data.StrokeColor.Blue, data.StrokeColor.Alpha);
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

            if (def.YLabeler is not null)
            {
                var tickGen = new ScottPlot.TickGenerators.NumericAutomatic
                {
                    LabelFormatter = def.YLabeler
                };
                plot.Axes.Left.TickGenerator = tickGen;
            }

            if (def.YMinLimit.HasValue || def.YMaxLimit.HasValue)
            {
                var limits = plot.Axes.GetLimits();
                var yMin = def.YMinLimit ?? limits.Bottom;
                var yMax = def.YMaxLimit ?? limits.Top;
                plot.Axes.SetLimitsY(yMin, yMax);
            }

            plots.Add(plot);
            multiplot.Subplots.Add(plot);
        }

        _minX = minX;
        _maxX = maxX;
        foreach (var subplot in plots)
        {
            subplot.Axes.SetLimitsX(minX, maxX);
        }

        multiplot.SharedAxes.ShareX(plots);
        ChartElement.Multiplot = multiplot;
        IsVisible = true;
        ChartElement.Refresh();
    }

    private class XOnlyMouseWheelZoom(ChartControl control) : IUserActionResponse
    {
        public double ZoomFraction { get; set; } = 0.15;

        public void ResetState(IPlotControl plotControl)
        {
        }

        public ResponseInfo Execute(IPlotControl plotControl, IUserAction userInput, KeyboardState keys)
        {
            if (userInput is MouseWheelUp up)
            {
                Plot? plot = plotControl.GetPlotAtPixel(up.Pixel);
                if (plot is null)
                    return ResponseInfo.NoActionRequired;

                double zoomIn = 1 + ZoomFraction;
                MouseAxisManipulation.MouseWheelZoom(plot, zoomIn, 1, up.Pixel, false);
                ClampXLimits(plot);
                return new ResponseInfo { RefreshNeeded = true };
            }

            if (userInput is MouseWheelDown down)
            {
                Plot? plot = plotControl.GetPlotAtPixel(down.Pixel);
                if (plot is null)
                    return ResponseInfo.NoActionRequired;

                double zoomOut = 1 / (1 + ZoomFraction);
                MouseAxisManipulation.MouseWheelZoom(plot, zoomOut, 1, down.Pixel, false);
                ClampXLimits(plot);
                return new ResponseInfo { RefreshNeeded = true };
            }

            return ResponseInfo.NoActionRequired;
        }
        
        private void ClampXLimits(Plot plot)
        {
            var limits = plot.Axes.GetLimits();
            var left = Math.Max(limits.Left, control._minX);
            var right = Math.Min(limits.Right, control._maxX);
            plot.Axes.SetLimitsX(left, right);
        }
    }
    
    public static void ApplySubplotStyle(Plot plot)
    {
        plot.FigureBackground.Color = new Color("#1c1c1e");
        plot.Axes.Color(new Color("#888888"));

        plot.Grid.XAxisStyle.FillColor1 = new Color("#888888").WithAlpha(10);
        plot.Grid.YAxisStyle.FillColor1 = new Color("#888888").WithAlpha(10);

        plot.Grid.XAxisStyle.MajorLineStyle.Color = Colors.White.WithAlpha(15);
        plot.Grid.YAxisStyle.MajorLineStyle.Color = Colors.White.WithAlpha(15);
        plot.Grid.XAxisStyle.MinorLineStyle.Color = Colors.White.WithAlpha(5);
        plot.Grid.YAxisStyle.MinorLineStyle.Color = Colors.White.WithAlpha(5);

        plot.Grid.XAxisStyle.MinorLineStyle.Width = 1;
        plot.Grid.YAxisStyle.MinorLineStyle.Width = 1;
            
        plot.Layout.Fixed(new PixelPadding(50, 16, 32, 50));
    }

}