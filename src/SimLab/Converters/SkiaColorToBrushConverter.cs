using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using SkiaSharp;

namespace SimLab.Converters;

public class SkiaColorToBrushConverter : IValueConverter
{
    public static readonly SkiaColorToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SKColor skColor)
        {
            return new SolidColorBrush(Color.FromArgb(skColor.Alpha, skColor.Red, skColor.Green, skColor.Blue));
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
