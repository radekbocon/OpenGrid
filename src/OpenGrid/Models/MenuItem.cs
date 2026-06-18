using System;
using Material.Icons;

namespace OpenGrid.Models;

public class MenuItem
{
    public required MaterialIconKind Icon { get; init; }
    public required string Label { get; init; }
    public required Type ViewModelType { get; init; }
}
