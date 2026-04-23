using System;

namespace SimLab.ViewModels;

public class MenuItem
{
    public required string Icon { get; init; }
    public required string Label { get; init; }
    public required Type ViewModelType { get; init; }
}
