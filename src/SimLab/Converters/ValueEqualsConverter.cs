using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace SimLab.Converters;

public class ValueEqualsConverter : IValueConverter
{
    public static readonly ValueEqualsConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null && parameter is null)
            return true;
        
        if (value is null || parameter is null)
            return false;

        if (parameter is string paramStr && value is IConvertible)
        {
            try
            {
                var convertedParam = System.Convert.ChangeType(paramStr, value.GetType(), culture);
                return value.Equals(convertedParam);
            }
            catch
            {
                return false;
            }
        }

        return value.Equals(parameter);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true)
        {
            if (parameter is string paramStr && targetType.IsValueType)
            {
                try
                {
                    return System.Convert.ChangeType(paramStr, targetType, culture);
                }
                catch
                {
                    return BindingOperations.DoNothing;
                }
            }
            return parameter;
        }
        return BindingOperations.DoNothing;
    }
}
