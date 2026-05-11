using Avalonia.Media;

namespace SimLab.Models;

public class DashboardInfo
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public IImage? Image { get; init; }
    public required string Id { get; init; }
    public bool IsSystem { get; init; }
    public required string DirectoryPath { get; init; }
}
