using System;
using System.Collections.Generic;
using Avalonia.Input;
using LiveChartsCore.SkiaSharpView.Avalonia;
using SimLab.ViewModels;

namespace SimLab.Views;

public class ChartInteractionHelper
{
    private bool _isPanning;
    private double _panStartX;
    private double _panStartMinX;
    private double _panStartMaxX;
    private CartesianChart? _panningChart;
    private readonly Func<ILapChartViewModel?> _getViewModel;

    public ChartInteractionHelper(Func<ILapChartViewModel?> getViewModel)
    {
        _getViewModel = getViewModel;
    }

    public void AttachEvents(IEnumerable<CartesianChart> charts)
    {
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
        var vm = _getViewModel();
        if (e.GetCurrentPoint(sender as InputElement).Properties.IsLeftButtonPressed && vm != null)
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
        var vm = _getViewModel();
        if (_isPanning && _panningChart != null && vm != null)
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
        var vm = _getViewModel();
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && vm != null)
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
