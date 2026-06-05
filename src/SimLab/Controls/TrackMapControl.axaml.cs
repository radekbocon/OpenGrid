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
using SimLab.Models;
using SkiaSharp;

namespace SimLab.Controls;

public partial class TrackMapControl : UserControl
{
    private const double SegmentDistance = 10.0;
    
    public static readonly StyledProperty<IReadOnlyList<TrackMapSeries>> SeriesProperty =
        AvaloniaProperty.Register<TrackMapControl, IReadOnlyList<TrackMapSeries>>(nameof(Series), []);

    public IReadOnlyList<TrackMapSeries> Series
    {
        get => GetValue(SeriesProperty);
        set => SetValue(SeriesProperty, value);
    }

    public TrackMapControl()
    {
        InitializeComponent();
        IsVisible = false;
        SizeChanged += (_, _) =>
        {
            PlotElement.Plot.Axes.AutoScale();
            ApplySquareLimits();
        }; 
        PlotElement.AddHandler(PointerWheelChangedEvent, PointerWheelHandler, RoutingStrategies.Tunnel);
        PlotElement.UserInputProcessor.RemoveAll<IUserActionResponse>();
        PlotElement.UserInputProcessor.UserActionResponses.Add(new MouseDragPan(StandardMouseButtons.Left));
        PlotElement.UserInputProcessor.UserActionResponses.Add(new TrackMapMouseWheelZoom(this));
#if DEBUG
        PlotElement.UserInputProcessor.UserActionResponses.Add(new DoubleClickBenchmark(StandardMouseButtons.Right));
#endif
        PlotElement.SizeChanged += (_, _) =>
        {
            if (IsVisible)
            {
                Dispatcher.UIThread.Post(ApplySquareLimits, DispatcherPriority.Background);
            }
        };
    }

    private void PointerWheelHandler(object? sender, PointerWheelEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            return;
        }

        var scrollViewer = PlotElement.FindAncestorOfType<ScrollViewer>();
        scrollViewer?.Offset += e.Delta * -40;
        e.Handled = true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SeriesProperty)
        {
            Dispatcher.UIThread.Post(UpdatePlot, DispatcherPriority.Background);
        }
    }

    private void UpdatePlot()
    {
        var seriesList = Series.ToList();

        PlotElement.Reset();

        if (seriesList.Count == 0)
        {
            IsVisible = false;
            return;
        }

        var plot = PlotElement.Plot;

        plot.FigureBackground.Color = Colors.Transparent;

        plot.Layout.Fixed(new PixelPadding(0, 0, 0, 0));

        foreach (var s in seriesList)
        {
            if (s.GasValues is not null && s.BrakeValues is not null && s.GasValues.Count > 0 && s.GasValues.Count == s.Xs.Count)
            {
                RenderColoredTrack(plot, s);
            }
            else
            {
                var xs = s.Xs.ToArray();
                var ys = s.Ys.ToArray();
                var scatter = plot.Add.Scatter(xs, ys);
                scatter.Color = new Color(s.Color.Red, s.Color.Green, s.Color.Blue, s.Color.Alpha);
                scatter.LineWidth = 6;
                scatter.MarkerSize = 0;
            }
        }

        plot.HideLegend();
        plot.HideGrid();
        plot.Axes.Frameless();

        if (seriesList.Count > 0)
        {
            plot.Axes.AutoScale();

            // First render to get pixel dimensions for aspect-correct limits
            PlotElement.Refresh();

            ApplySquareLimits();
        }

        IsVisible = true;
        PlotElement.Refresh();
    }

    private static void RenderColoredTrack(Plot plot, TrackMapSeries s)
    {
        var xs = s.Xs;
        var ys = s.Ys;
        var gas = s.GasValues!;
        var brake = s.BrakeValues!;

        var n = xs.Count;
        var segmentStarts = new List<int> { 0 };
        double accumulated = 0;

        for (int i = 1; i < n; i++)
        {
            var dx = xs[i] - xs[i - 1];
            var dy = ys[i] - ys[i - 1];
            accumulated += Math.Sqrt(dx * dx + dy * dy);

            if (accumulated >= SegmentDistance)
            {
                segmentStarts.Add(i);
                accumulated = 0;
            }
        }

        if (segmentStarts[^1] != n - 1)
            segmentStarts.Add(n - 1);

        for (var si = 0; si < segmentStarts.Count - 1; si++)
        {
            var i = segmentStarts[si];
            var end = Math.Min(segmentStarts[si + 1] + 2, n);

            float avgGas = 0, avgBrake = 0;
            var count = end - i - 1;
            for (var j = i; j < end - 1; j++)
            {
                avgGas += gas[j];
                avgBrake += brake[j];
            }
            avgGas /= count;
            avgBrake /= count;

            var spanXs = new double[end - i];
            var spanYs = new double[end - i];
            for (int j = i; j < end; j++)
            {
                spanXs[j - i] = xs[j];
                spanYs[j - i] = ys[j];
            }

            var skColor = GasBrakeHeatColor(avgGas, avgBrake);
            var scatter = plot.Add.ScatterLine(spanXs, spanYs);
            scatter.Color = new Color(skColor.Red, skColor.Green, skColor.Blue, skColor.Alpha);
            scatter.LineWidth = 6;
            scatter.MarkerSize = 0;
        }
    }

    private static SKColor GasBrakeHeatColor(float gas, float brake)
    {
        var maxInput = Math.Max(gas, brake);
        if (maxInput < 0.01f)
            return new SKColor(40, 40, 40);

        var r = (byte)Math.Clamp(40 + brake * 215, 0, 255);
        var g = (byte)Math.Clamp(40 + gas * 215, 0, 255);

        return new SKColor(r, g, 0);
    }

    private void ApplySquareLimits()
    {
        var plot = PlotElement.Plot;
        var limits = plot.Axes.GetLimits();
        var rangeX = limits.Right - limits.Left;
        var rangeY = limits.Top - limits.Bottom;
        if (rangeX <= 0 || rangeY <= 0) return;

        var pixelWidth = PlotElement.Bounds.Width;
        var pixelHeight = PlotElement.Bounds.Height;
        if (pixelWidth <= 0 || pixelHeight <= 0) return;

        var pixelAspect = pixelWidth / pixelHeight;
        var dataAspect = rangeX / rangeY;

        var centerX = (limits.Right + limits.Left) / 2;
        var centerY = (limits.Top + limits.Bottom) / 2;

        if (dataAspect > pixelAspect)
        {
            var newRangeY = rangeX / pixelAspect;
            plot.Axes.SetLimitsX(limits.Left, limits.Right);
            plot.Axes.SetLimitsY(centerY - newRangeY / 2, centerY + newRangeY / 2);
        }
        else
        {
            var newRangeX = rangeY * pixelAspect;
            plot.Axes.SetLimitsX(centerX - newRangeX / 2, centerX + newRangeX / 2);
            plot.Axes.SetLimitsY(limits.Bottom, limits.Top);
        }
    }

    private class TrackMapMouseWheelZoom(TrackMapControl owner) : IUserActionResponse
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
                MouseAxisManipulation.MouseWheelZoom(plot, zoomIn, zoomIn, up.Pixel, true);
                owner.ApplySquareLimits();
                return new ResponseInfo { RefreshNeeded = true };
            }

            if (userInput is MouseWheelDown down)
            {
                Plot? plot = plotControl.GetPlotAtPixel(down.Pixel);
                if (plot is null)
                    return ResponseInfo.NoActionRequired;

                double zoomOut = 1 / (1 + ZoomFraction);
                MouseAxisManipulation.MouseWheelZoom(plot, zoomOut, zoomOut, down.Pixel, true);
                owner.ApplySquareLimits();
                return new ResponseInfo { RefreshNeeded = true };
            }

            return ResponseInfo.NoActionRequired;
        }
    }
}
