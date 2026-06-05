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
namespace SimLab.Controls;

public partial class TrackMapControl : UserControl
{
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
                var coloredTrack = new ColoredTrack(s.Xs, s.Ys, s.GasValues, s.BrakeValues);
                plot.PlottableList.Add(coloredTrack);
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
