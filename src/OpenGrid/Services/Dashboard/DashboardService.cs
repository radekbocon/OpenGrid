using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.Json;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using OpenGrid.Models;

namespace OpenGrid.Services.Dashboard;

public sealed class DashboardService : IDashboardService
{
    private static readonly string UserDashboardsPath = Path.Combine(Program.AppDataDirectory, "dashboards");

    public ObservableCollection<DashboardInfo> Dashboards { get; } = [];

    public DashboardService()
    {
        Directory.CreateDirectory(UserDashboardsPath);
        Scan();
    }

    public void Scan()
    {
        Dashboards.Clear();

        ScanBuiltinDashboards();
        ScanUserDashboards();
    }

    public DashboardInfo? GetById(string id)
        => Dashboards.FirstOrDefault(d =>
            string.Equals(d.Id, id, StringComparison.OrdinalIgnoreCase));

    private void ScanBuiltinDashboards()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string prefix = "OpenGrid.Assets.Dashboards.";
        var dashboardFiles = new Dictionary<string, List<(string resourceName, string relativePath)>>();

        foreach (var name in assembly.GetManifestResourceNames())
        {
            if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var rest = name[prefix.Length..];
            var dotIndex = rest.IndexOf('.');
            if (dotIndex <= 0)
                continue;

            var id = rest[..dotIndex];
            var relativePath = rest[(dotIndex + 1)..];

            if (!dashboardFiles.ContainsKey(id))
                dashboardFiles[id] = [];
            dashboardFiles[id].Add((name, relativePath));
        }

        foreach (var (id, files) in dashboardFiles)
        {
            var metadataPath = files.FirstOrDefault(f =>
                string.Equals(f.relativePath, "metadata.json", StringComparison.OrdinalIgnoreCase)).resourceName;
            if (metadataPath is null)
                continue;

            DashboardMetadata? metadata;
            try
            {
                using var stream = assembly.GetManifestResourceStream(metadataPath);
                if (stream is null)
                    continue;
                using var reader = new StreamReader(stream);
                metadata = JsonSerializer.Deserialize<DashboardMetadata>(reader.ReadToEnd());
            }
            catch
            {
                continue;
            }

            if (metadata is null)
                continue;

            IImage? image = null;
            var previewResource = files.FirstOrDefault(f =>
                string.Equals(f.relativePath, "preview.png", StringComparison.OrdinalIgnoreCase)).resourceName;
            if (previewResource is not null)
            {
                try
                {
                    using var stream = assembly.GetManifestResourceStream(previewResource);
                    if (stream is not null)
                    {
                        using var ms = new MemoryStream();
                        stream.CopyTo(ms);
                        ms.Position = 0;
                        image = new Bitmap(ms);
                    }
                }
                catch
                {
                    // ignore broken preview
                }
            }

            Dashboards.Add(new DashboardInfo
            {
                Id = id,
                Name = metadata.Name,
                Description = metadata.Description,
                DirectoryPath = $"avares://OpenGrid/Assets/Dashboards/{id}/",
                IsSystem = true,
                Image = image,
            });
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
            Image = image,
        };
    }
}
