using System;
using System.Globalization;

namespace SimLab.Models;

public readonly record struct Car
{
    public string Key { get; }
    public string DisplayName => CleanCarName(Key);
    public static Car Unknown { get; } = new("unknown_car");

    private Car(string key)
    {
        Key = key;
    }
    
    public static Car Create(string? key)
    {
        return string.IsNullOrWhiteSpace(key) ? Unknown : new Car(key);
    }


    public override string ToString() => DisplayName;

    private static string CleanCarName(string key)
    {
        var name = key.Trim();

        if (name.StartsWith("ks_", StringComparison.OrdinalIgnoreCase))
        {
            name = name[3..];
        }

        name = name.Replace("_", " ");

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());
    }
}