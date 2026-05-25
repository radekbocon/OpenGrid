using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Material.Icons;

namespace SimLab.Converters;

public class ExpandedToIconConverter : IValueConverter
{
    public static readonly ExpandedToIconConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isExpanded)
        {
            return isExpanded ? MaterialIconKind.ArrowDropUp : MaterialIconKind.ArrowDropDown;
        }
        return MaterialIconKind.ArrowDropDown;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
