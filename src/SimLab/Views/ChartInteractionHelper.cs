using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Input;
using ScottPlot.Avalonia;
using SimLab.ViewModels;

namespace SimLab.Views;

public class ChartInteractionHelper
{
    private bool _isPanning;
    private double _panStartX;
    private double _panStartMinX;
    private double _panStartMaxX;
    private AvaPlot? _panningChart;
    private readonly Func<ILapChartViewModel?> _getViewModel;
    private List<AvaPlot> _charts = [];

    public ChartInteractionHelper(Func<ILapChartViewModel?> getViewModel)
    {
        _getViewModel = getViewModel;
    }

    public void AttachEvents(IEnumerable<AvaPlot> charts)
    {
        _charts = new List<AvaPlot>(charts);
        foreach (var chart in _charts)
        {
            chart.AddHandler(InputElement.PointerWheelChangedEvent, Chart_PointerWheelChanged, handledEventsToo: true);
            chart.AddHandler(InputElement.PointerPressedEvent, Chart_PointerPressed, handledEventsToo: true);
            chart.AddHandler(InputElement.PointerMovedEvent, Chart_PointerMoved, handledEventsToo: true);
            chart.AddHandler(InputElement.PointerReleasedEvent, Chart_PointerReleased, handledEventsToo: true);
            chart.AddHandler(InputElement.PointerCaptureLostEvent, Chart_PointerCaptureLost, handledEventsToo: true);
        }
    }

    private void UpdateAllChartLimits(double minX, double maxX)
    {
        foreach (var chart in _charts)
        {
            chart.Plot.Axes.SetLimitsX(minX, maxX);
            chart.Refresh();
        }
    }

    private void Chart_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var vm = _getViewModel();
        if (e.GetCurrentPoint(sender as InputElement).Properties.IsLeftButtonPressed && vm != null)
        {
            _isPanning = true;
            _panningChart = sender as AvaPlot;
            if (_panningChart != null)
            {
                e.Pointer.Capture(_panningChart);
                _panStartX = e.GetPosition(_panningChart).X;
                _panStartMinX = vm.MinX;
                _panStartMaxX = vm.MaxX;
            }
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
                UpdateAllChartLimits(vm.MinX, vm.MaxX);
            }
        }
    }

    private void Chart_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_panningChart != null)
        {
            e.Pointer.Capture(null);
        }
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
            UpdateAllChartLimits(vm.MinX, vm.MaxX);
            e.Handled = true;
        }
        else
        {
            e.Handled = false;
        }
    }
}
