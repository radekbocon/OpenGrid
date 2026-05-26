using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

namespace SimLab.ViewModels;

public class ChartData
{
    public string Name { get; set; }
    public double[] Xs { get; set; }
    public double[] Ys { get; set; }
    public SKColor StrokeColor { get; set; }
    public float StrokeThickness { get; set; }
    public bool IsStep { get; set; }
    public bool IsDashed { get; set; }

    public ChartData(string name, IEnumerable<double> xs, IEnumerable<double> ys, SKColor strokeColor, float strokeThickness = 2, bool isStep = false, bool isDashed = false)
    {
        Name = name;
        Xs = xs.ToArray();
        Ys = ys.ToArray();
        StrokeColor = strokeColor;
        StrokeThickness = strokeThickness;
        IsStep = isStep;
        IsDashed = isDashed;
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
}
