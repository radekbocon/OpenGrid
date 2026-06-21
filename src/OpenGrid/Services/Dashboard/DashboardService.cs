using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using OpenGrid.Models;

namespace OpenGrid.Services;

public sealed class DashboardService : IDashboardService
{
    private static readonly Assembly Assembly = Assembly.GetExecutingAssembly();
    private static readonly string BuiltinExtractPath =
        Path.Combine(Program.AppDataDirectory, "dashboards-builtin");
    private static readonly string UserDashboardsPath =
        Path.Combine(Program.AppDataDirectory, "dashboards");

    private static readonly Regex DashboardResourceRegex = new(
        @"^OpenGrid\.Assets\.Dashboards\.([\w.-]+?)\.(.+)$",
        RegexOptions.IgnoreCase);

    public ObservableCollection<DashboardInfo> Dashboards { get; } = [];

    public DashboardService()
    {
        Directory.CreateDirectory(BuiltinExtractPath);
        Directory.CreateDirectory(UserDashboardsPath);
        Scan();
    }

    public void Scan()
    {
        Dashboards.Clear();

        ExtractAndScanBuiltin();
        ScanUserDashboards();
    }

    public IEnumerable<DashboardInfo> GetByType(DashboardType type)
        => Dashboards.Where(d => d.Type == type);

    public DashboardInfo? GetById(string id)
        => Dashboards.FirstOrDefault(d =>
            string.Equals(d.Id, id, StringComparison.OrdinalIgnoreCase));

    private void ExtractAndScanBuiltin()
    {
        var resourceNames = Assembly.GetManifestResourceNames();
        var dashboardFiles = new Dictionary<string, List<(string resourceName, string fileName)>>();

        foreach (var name in resourceNames)
        {
            var match = DashboardResourceRegex.Match(name);
            if (!match.Success)
                continue;

            var id = match.Groups[1].Value;
            var fileName = match.Groups[2].Value;

            if (!dashboardFiles.ContainsKey(id))
                dashboardFiles[id] = [];
            dashboardFiles[id].Add((name, fileName));
        }

        foreach (var (id, files) in dashboardFiles)
        {
            var targetDir = Path.Combine(BuiltinExtractPath, id);
            Directory.CreateDirectory(targetDir);

            foreach (var (resourceName, fileName) in files)
            {
                var filePath = Path.Combine(targetDir, fileName);

                using var stream = Assembly.GetManifestResourceStream(resourceName);
                if (stream is null)
                    continue;

                var dir = Path.GetDirectoryName(filePath);
                if (dir is not null)
                    Directory.CreateDirectory(dir);

                using var fileStream = File.Create(filePath);
                stream.CopyTo(fileStream);
            }

            var dashboard = LoadDashboardFromDirectory(targetDir, isSystem: true);
            if (dashboard is not null)
                Dashboards.Add(dashboard);
        }
    }

    private void ScanUserDashboards()
    {
        if (!Directory.Exists(UserDashboardsPath))
            return;

        foreach (var dir in Directory.GetDirectories(UserDashboardsPath))
        {
            var dashboard = LoadDashboardFromDirectory(dir, isSystem: false);
            if (dashboard is not null)
                Dashboards.Add(dashboard);
        }
    }

    private static DashboardInfo? LoadDashboardFromDirectory(string dir, bool isSystem)
    {
        var metadataPath = Path.Combine(dir, "metadata.json");
        if (!File.Exists(metadataPath))
            return null;

        DashboardMetadata? metadata;
        try
        {
            var json = File.ReadAllText(metadataPath);
            metadata = JsonSerializer.Deserialize<DashboardMetadata>(json);
        }
        catch
        {
            return null;
        }

        if (metadata is null)
            return null;

        var id = Path.GetFileName(dir);
        var type = string.Equals(metadata.Type, "overlay", StringComparison.OrdinalIgnoreCase)
            ? DashboardType.Overlay
            : DashboardType.Dashboard;

        IImage? image = null;
        var previewPath = Path.Combine(dir, "preview.png");
        if (File.Exists(previewPath))
        {
            try
            {
                image = new Bitmap(previewPath);
            }
            catch
            {
                // ignore broken preview
            }
        }

        return new DashboardInfo
        {
            Id = id,
            Name = metadata.Name,
            Description = metadata.Description,
            DirectoryPath = dir,
            IsSystem = isSystem,
            Type = type,
            Image = image
        };
    }
}
