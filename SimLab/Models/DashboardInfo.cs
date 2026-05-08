using System;
using SimLab.Views;

namespace SimLab.Models;

public class DashboardInfo
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Icon { get; init; }
    public required DashboardStyle Style { get; init; }
}
