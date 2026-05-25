using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LiveChartsCore.Defaults;
using LiveChartsCore.Painting;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace SimLab.ViewModels;

public class ChartData
{
    private readonly List<ObservablePoint> _fullData;
    
    public string Name { get; set; }

    public ObservableCollection<ObservablePoint> Points { get; set; }

    public Paint Stroke { get; set; }

    public ChartData(string name, IEnumerable<ObservablePoint> values, Paint stroke)
    {
        _fullData = values.ToList();
        
        Name = name;
        Points = new ObservableCollection<ObservablePoint>(_fullData);
        Stroke = stroke;
    }

    public void Resample(int widthPixels, double maxX, double minX)
    {
        const int pixelsPerPoint = 2;

        var maxPoints = widthPixels / pixelsPerPoint;

        var visible = _fullData
            .Where(p => p.X >= minX && p.X <= maxX)
            .ToList();

        if (visible.Count <= maxPoints)
        {
            Points.Clear();
            foreach (var p in visible)
                Points.Add(p);

            return;
        }

        var step = (double)visible.Count / maxPoints;

        var resampled = new List<ObservablePoint>(maxPoints);

        for (var i = 0; i < maxPoints; i++)
        {
            var index = (int)(i * step);
            if (index >= visible.Count)
            {
                index = visible.Count - 1;
            }

            resampled.Add(visible[index]);
        }

        Points.Clear();
        foreach (var p in resampled)
        {
            Points.Add(p);
        }
    }
}

public static class ColorPalette
{
    private static readonly SKColor[] Colors = 
    {
        SKColors.DodgerBlue,
        SKColors.Crimson,
        SKColors.LimeGreen,
        SKColors.DarkOrange,
        SKColors.MediumPurple,
        SKColors.DeepPink,
        SKColors.Teal,
        SKColors.Gold,
        SKColors.RoyalBlue,
        SKColors.Firebrick,
        SKColors.SeaGreen,
        SKColors.SandyBrown,
        SKColors.SlateBlue,
        SKColors.HotPink,
        SKColors.DarkCyan,
        SKColors.OrangeRed
    };

    public static SKColor GetColor(int index)
    {
        return Colors[index % Colors.Length];
    }

    public static SolidColorPaint GetPaint(int index, float strokeWidth = 2)
    {
        return new SolidColorPaint(GetColor(index), strokeWidth);
    }
}
