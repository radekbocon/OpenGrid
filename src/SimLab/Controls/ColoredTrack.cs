using System;
using System.Collections.Generic;
using SkiaSharp;
using ScottPlot;

namespace SimLab.Controls;

public class ColoredTrack : IPlottable, IDisposable
{
    public bool IsVisible { get; set; } = true;
    public IAxes Axes { get; set; } = new Axes();

    private readonly IReadOnlyList<double> _xs;
    private readonly IReadOnlyList<double> _ys;
    private readonly float _lineWidth;

    private readonly int[] _segmentStarts;
    private readonly int[] _segmentEnds;
    private readonly SKColor[] _segmentColors;
    private readonly SKPaint _paint;

    public ColoredTrack(
        IReadOnlyList<double> xs,
        IReadOnlyList<double> ys,
        IReadOnlyList<float>? gas,
        IReadOnlyList<float>? brake,
        double segmentDistance = 10.0,
        float lineWidth = 6)
    {
        _xs = xs;
        _ys = ys;
        _lineWidth = lineWidth;
        _paint = new SKPaint
        {
            IsStroke = true,
            StrokeWidth = lineWidth,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };

        int n = xs.Count;
        var starts = new List<int> { 0 };
        double accumulated = 0;

        bool hasGasBrake = gas is not null && brake is not null && gas.Count >= n && brake.Count >= n;

        for (int i = 1; i < n; i++)
        {
            var dx = xs[i] - xs[i - 1];
            var dy = ys[i] - ys[i - 1];
            accumulated += Math.Sqrt(dx * dx + dy * dy);

            if (accumulated >= segmentDistance)
            {
                starts.Add(i);
                accumulated = 0;
            }
        }

        if (starts[^1] != n - 1)
            starts.Add(n - 1);

        int segmentCount = starts.Count - 1;
        _segmentStarts = new int[segmentCount];
        _segmentEnds = new int[segmentCount];
        _segmentColors = new SKColor[segmentCount];

        for (int si = 0; si < segmentCount; si++)
        {
            _segmentStarts[si] = starts[si];
            _segmentEnds[si] = Math.Min(starts[si + 1] + 2, n);

            if (hasGasBrake)
            {
                float avgGas = 0, avgBrake = 0;
                var count = _segmentEnds[si] - _segmentStarts[si] - 1;
                for (var j = _segmentStarts[si]; j < _segmentEnds[si] - 1; j++)
                {
                    avgGas += gas![j];
                    avgBrake += brake![j];
                }
                avgGas /= count;
                avgBrake /= count;

                _segmentColors[si] = GasBrakeHeatColor(avgGas, avgBrake);
            }
            else
            {
                _segmentColors[si] = new SKColor(40, 40, 40);
            }
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

    public AxisLimits GetAxisLimits()
    {
        if (_xs.Count == 0)
            return AxisLimits.NoLimits;

        double minX = _xs[0], maxX = _xs[0];
        double minY = _ys[0], maxY = _ys[0];

        for (int i = 1; i < _xs.Count; i++)
        {
            if (_xs[i] < minX) minX = _xs[i];
            if (_xs[i] > maxX) maxX = _xs[i];
            if (_ys[i] < minY) minY = _ys[i];
            if (_ys[i] > maxY) maxY = _ys[i];
        }

        return new AxisLimits(minX, maxX, minY, maxY);
    }

    public IEnumerable<LegendItem> LegendItems => [];

    public void Dispose()
    {
        _paint.Dispose();
    }

    public void Render(RenderPack rp)
    {
        if (_segmentStarts.Length == 0)
            return;

        for (int si = 0; si < _segmentStarts.Length; si++)
        {
            _paint.Color = _segmentColors[si];

            int start = _segmentStarts[si];
            int end = _segmentEnds[si];

            using var path = new SKPath();
            path.MoveTo(Axes.GetPixelX(_xs[start]), Axes.GetPixelY(_ys[start]));
            for (int j = start + 1; j < end; j++)
            {
                path.LineTo(Axes.GetPixelX(_xs[j]), Axes.GetPixelY(_ys[j]));
            }

            rp.Canvas.DrawPath(path, _paint);
        }
    }
}
