using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using LiveChartsCore.SkiaSharpView.Avalonia;
using SimLab.Controls;
using SimLab.ViewModels;

namespace SimLab.Views;

public partial class SessionDetailsView : UserControl
{
    private bool _isPanning;
    private double _panStartX;
    private double _panStartMinX;
    private double _panStartMaxX;
    private CartesianChart? _panningChart;

    public SessionDetailsView()
    {
        InitializeComponent();
        List<CartesianChart> charts = [InputsChart.Chart, SteeringChart.Chart, SpeedChart.Chart, GearChart.Chart, RpmChart.Chart];
        foreach (var chart in charts)
        {
            chart.PointerWheelChanged += Chart_PointerWheelChanged;
            chart.PointerPressed += Chart_PointerPressed;
            chart.PointerMoved += Chart_PointerMoved;
            chart.PointerReleased += Chart_PointerReleased;
            chart.PointerCaptureLost += Chart_PointerCaptureLost;
        }
    }

    private void Chart_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(sender as InputElement).Properties.IsLeftButtonPressed &&
            DataContext is SessionDetailsViewModel vm)
        {
            _isPanning = true;
            _panningChart = sender as CartesianChart;
            _panStartX = e.GetPosition(_panningChart).X;
            _panStartMinX = vm.MinX;
            _panStartMaxX = vm.MaxX;
            e.Handled = true;
        }
    }

    private void Chart_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isPanning && _panningChart != null && DataContext is SessionDetailsViewModel vm)
        {
            var currentX = e.GetPosition(_panningChart).X;
            var deltaPixels = currentX - _panStartX;
            var chartWidth = _panningChart.Bounds.Width;
            if (chartWidth > 0)
            {
                var range = _panStartMaxX - _panStartMinX;
                var dataDelta = -deltaPixels * range / chartWidth;
                var newMinX = _panStartMinX + dataDelta;
                var newMaxX = _panStartMaxX + dataDelta;
                vm.MinX = Math.Max(vm.DataMinX, newMinX);
                vm.MaxX = Math.Min(vm.DataMaxX, newMaxX);
            }
        }
    }

    private void Chart_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isPanning = false;
        _panningChart = null;
    }

    private void Chart_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        _isPanning = false;
        _panningChart = null;
    }

    private void Chart_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && DataContext is SessionDetailsViewModel vm)
        {
            var range = vm.MaxX - vm.MinX;
            var factor = e.Delta.Y > 0 ? 0.9 : 1.1;
            var newRange = range * factor;
            var center = (vm.MaxX + vm.MinX) / 2;
            vm.MinX = Math.Max(vm.DataMinX, center - newRange / 2);
            vm.MaxX = Math.Min(vm.DataMaxX, center + newRange / 2);
            e.Handled = true;
        }
    }
}
