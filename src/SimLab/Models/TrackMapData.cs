using System.Collections.Generic;
using SkiaSharp;

namespace SimLab.Models;

public record TrackMapSeries
{
    public required string Name { get; init; }
    public required IReadOnlyList<double> Xs { get; init; }
    public required IReadOnlyList<double> Ys { get; init; }
    public SKColor Color { get; init; }
}
