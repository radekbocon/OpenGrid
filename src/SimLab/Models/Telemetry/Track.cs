using System;
using System.Globalization;

namespace SimLab.Models.Telemetry;

public readonly record struct Track
{
    public string Key { get; }
    public string DisplayName => CleanTrackName(Key);
    public static Track Unknown { get; } = new("unknown_track");

    private Track(string key)
    {
        Key = key;
    }
    
    public static Track Create(string? key)
    {
        return string.IsNullOrWhiteSpace(key) ? Unknown : new Track(key);
    }


    public override string ToString() => DisplayName;

    private static string CleanTrackName(string key)
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