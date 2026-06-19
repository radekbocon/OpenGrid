using Avalonia.Media;

namespace OpenGrid.Models;

public enum DashboardType
{
    Dashboard,
    Overlay
}

public class DashboardInfo
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public IImage? Image { get; init; }
    public required string Id { get; init; }
    public bool IsSystem { get; init; }
    public required string DirectoryPath { get; init; }
    public DashboardType Type { get; init; } = DashboardType.Dashboard;
    public string Url { get; set; } = "";
}
