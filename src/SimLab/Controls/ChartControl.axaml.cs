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
using ScottPlot.Plottables;
using SimLab.Models;

namespace SimLab.Controls;

public partial class ChartControl : UserControl
{
    private const int SubplotHeight = 250;
    
    private double _minX;
    private double _maxX;
    private List<CrosshairState>? _crosshairStates;

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
        ChartElement.PointerMoved += OnChartPointerMoved;
        ChartElement.PointerExited += OnChartPointerExited;
    }

    private void PointerWheelHandler(object? sender, PointerWheelEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            return;
        }

        var scrollViewer = ChartElement.FindAncestorOfType<ScrollViewer>();
        scrollViewer?.Offset += e.Delta * -40;
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

        _crosshairStates = [];
        for (int i = 0; i < definitions.Count; i++)
        {
            var crosshair = plots[i].Add.Crosshair(0, 0);
            crosshair.IsVisible = false;
            crosshair.HorizontalLine.IsVisible = false;
            crosshair.LineColor = Colors.Gray.WithAlpha(0.8);
            crosshair.LineWidth = 1;

            var label = plots[i].Add.Text("", 0, 0);
            label.IsVisible = false;
            label.LabelFontSize = 12;
            label.LabelFontColor = Colors.White;
            label.LabelBackgroundColor = Colors.Black.WithAlpha(180);
            label.LabelPadding = 4;
            label.LabelBold = true;

            _crosshairStates.Add(new CrosshairState
            {
                Plot = plots[i],
                Definition = definitions[i],
                SeriesList = [.. definitions[i].Series],
                Crosshair = crosshair,
                Label = label
            });
        }

        multiplot.SharedAxes.ShareX(plots);
        ChartElement.Multiplot = multiplot;
        IsVisible = true;
        ChartElement.Refresh();
    }
    
    private static void ApplySubplotStyle(Plot plot)
    {
        // transparent causes some flickering
        plot.FigureBackground.Color = GetThemeColor("MaterialCardBackgroundColor", "#888888");

        var bodyColor = GetThemeColor("MaterialBodyColor", "#888888");

        plot.Axes.Color(bodyColor.WithAlpha(150));

        plot.Grid.XAxisStyle.FillColor1 = bodyColor.WithAlpha(10);
        plot.Grid.YAxisStyle.FillColor1 = bodyColor.WithAlpha(10);

        plot.Grid.XAxisStyle.MajorLineStyle.Color = bodyColor.WithAlpha(15);
        plot.Grid.YAxisStyle.MajorLineStyle.Color = bodyColor.WithAlpha(15);
        plot.Grid.XAxisStyle.MinorLineStyle.Color = bodyColor.WithAlpha(5);
        plot.Grid.YAxisStyle.MinorLineStyle.Color = bodyColor.WithAlpha(5);

        plot.Grid.XAxisStyle.MinorLineStyle.Width = 1;
        plot.Grid.YAxisStyle.MinorLineStyle.Width = 1;

        plot.Layout.Fixed(new PixelPadding(50, 16, 32, 50));
    }

    private static Color GetThemeColor(string key, string fallbackHex)
    {
        var app = Application.Current;
        if (app is not null && app.TryFindResource(key, app.ActualThemeVariant, out var value) && value is Avalonia.Media.Color color)
        {
            return new Color(color.R, color.G, color.B, color.A);
        }
        return new Color(fallbackHex);
    }

    private void OnChartPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_crosshairStates is null || _crosshairStates.Count == 0)
            return;

        var point = e.GetPosition(ChartElement);
        var pixel = new Pixel((float)point.X, (float)point.Y);

        var activePlot = ChartElement.GetPlotAtPixel(pixel);
        if (activePlot is null)
            return;

        var coordinates = activePlot.GetCoordinates(pixel);
        double x = coordinates.X;

        foreach (var state in _crosshairStates)
        {
            double y = GetYValueAtX(state.SeriesList, x);

            state.Crosshair.X = x;
            state.Crosshair.Y = y;
            state.Crosshair.IsVisible = true;

            state.Label.LabelText = FormatCrosshairLabel(state, x);
            var yBottom = state.Plot.Axes.GetLimits().Bottom;
            state.Label.Location = new ScottPlot.Coordinates(x, yBottom);
            state.Label.LabelOffsetY = -22;
            state.Label.LabelAlignment = Alignment.LowerLeft;
            state.Label.IsVisible = true;
        }

        ChartElement.Refresh();
    }

    private void OnChartPointerExited(object? sender, PointerEventArgs e)
    {
        if (_crosshairStates is null)
            return;

        foreach (var state in _crosshairStates)
        {
            state.Crosshair.IsVisible = false;
            state.Label.IsVisible = false;
        }

        ChartElement.Refresh();
    }

    private static double GetYValueAtX(List<ChartData> seriesList, double x)
    {
        if (seriesList.Count == 0)
            return 0;

        return InterpolateY(seriesList[0].Xs, seriesList[0].Ys, x);
    }

    private static double InterpolateY(double[] xs, double[] ys, double x)
    {
        if (xs.Length == 0)
            return 0;

        if (x <= xs[0])
            return ys[0];
        if (x >= xs[^1])
            return ys[^1];

        int index = Array.BinarySearch(xs, x);
        if (index >= 0)
            return ys[index];

        int next = ~index;
        if (next <= 0)
            return ys[0];
        if (next >= xs.Length)
            return ys[^1];

        double x0 = xs[next - 1];
        double x1 = xs[next];
        double t = (x - x0) / (x1 - x0);
        return ys[next - 1] + t * (ys[next] - ys[next - 1]);
    }

    private static string FormatCrosshairLabel(CrosshairState state, double x)
    {
        var labeler = state.Definition.YLabeler;

        if (state.SeriesList.Count == 1)
        {
            double y = InterpolateY(state.SeriesList[0].Xs, state.SeriesList[0].Ys, x);
            return labeler?.Invoke(y) ?? y.ToString("F2");
        }

        var parts = new List<string>();
        foreach (var series in state.SeriesList)
        {
            double y = InterpolateY(series.Xs, series.Ys, x);
            string formatted = labeler?.Invoke(y) ?? y.ToString("F2");
            string name = string.IsNullOrEmpty(series.Name) ? $"Series {parts.Count + 1}" : series.Name;
            parts.Add($"{name}: {formatted}");
        }

        return string.Join("\n", parts);
    }

    private class CrosshairState
    {
        public required Plot Plot { get; init; }
        public required SubplotDefinition Definition { get; init; }
        public required List<ChartData> SeriesList { get; init; }
        public required Crosshair Crosshair { get; init; }
        public required Text Label { get; init; }
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
}