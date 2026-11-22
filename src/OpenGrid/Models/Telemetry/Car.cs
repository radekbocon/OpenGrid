using System;
using System.Globalization;

namespace OpenGrid.Models.Telemetry;

public readonly record struct Car
{
    public string Name { get; }
    public static Car Unknown { get; } = new("Unknown car");

    private Car(string name)
    {
        Name = name;
    }
    
    public static Car Create(string? key)
    {
        return string.IsNullOrWhiteSpace(key) ? Unknown : new Car(key);
    }

    public override string ToString()
    {
        return Name;
    }
}