using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Styling;

namespace SimLab.Models;

public record ThemeInfo
{
    public required string Name { get; init; }
    public string? FilePath { get; init; }
    public required ThemeVariant Variant { get; init; }
    public required IReadOnlyDictionary<string, Color> Colors { get; init; }

    public Color? PrimaryColor =>
        Colors.TryGetValue("MaterialPrimaryMidBrush", out var c) ? c : null;
}
