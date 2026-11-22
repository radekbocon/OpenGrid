using System;
using System.Globalization;

namespace OpenGrid.Models.Telemetry;

public readonly record struct Track
{
    public string Name { get; }
    public static Track Unknown { get; } = new("Unknown track");

    private Track(string name)
    {
        Name = name;
    }
    
    public static Track Create(string? name)
    {
        return string.IsNullOrWhiteSpace(name) ? Unknown : new Track(name);
    }

    public override string ToString()
    {
        return Name;
    }
}