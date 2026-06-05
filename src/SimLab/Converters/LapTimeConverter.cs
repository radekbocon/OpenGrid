using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SimLab.Converters;

public class LapTimeConverter : IValueConverter
{
    public static string Format(TimeSpan lapTime)
    {
        if (lapTime.TotalSeconds < 60)
        {
            return lapTime.ToString(@"ss\.fff");
        }
        
        return lapTime.ToString(@"mm\:ss\.fff");
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not TimeSpan { Ticks: > 0 } lapTime 
            ? "--:--" 
            : Format(lapTime);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}