using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Serilog;
using SimLab.Models;
using SkiaSharp;

namespace SimLab.Services;

public class DashboardRepository : IDashboardRepository
{
    private readonly string _systemDir;
    private readonly string _userDir;
    private List<DashboardInfo>? _cached;

    public DashboardRepository()
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        _systemDir = Path.Combine(Program.AppDataDirectory, "Dashboards");
        _userDir = Path.Combine(documents, "SimLab", "Dashboards");

        EnsureSystemDashboards();
        EnsureUserDirectory();
    }

    public IReadOnlyList<DashboardInfo> GetAllDashboards()
    {
        if (_cached is not null) return _cached;
        _cached = [];
        _cached.AddRange(GetSystemDashboards());
        _cached.AddRange(GetUserDashboards());
        return _cached;
    }

    public IReadOnlyList<DashboardInfo> GetSystemDashboards()
    {
        return LoadFromDirectory(_systemDir, isSystem: true);
    }

    public IReadOnlyList<DashboardInfo> GetUserDashboards()
    {
        return LoadFromDirectory(_userDir, isSystem: false);
    }

    public DashboardInfo? GetById(string id)
    {
        return GetAllDashboards().FirstOrDefault(d => d.Id == id);
    }

    public string GetDashboardDirectory(DashboardInfo dashboard)
    {
        return dashboard.DirectoryPath;
    }

    private static List<DashboardInfo> LoadFromDirectory(string directory, bool isSystem)
    {
        var result = new List<DashboardInfo>();
        if (!Directory.Exists(directory)) return result;

        foreach (var subDir in Directory.EnumerateDirectories(directory))
        {
            var htmlFile = Path.Combine(subDir, "dashboard.html");
            var metaFile = Path.Combine(subDir, "metadata.json");
            var imageFile = Path.Combine(subDir, "image.png");
            if (!File.Exists(htmlFile)) continue;

            var metadata = LoadMetadata(metaFile);
            var dirName = Path.GetFileName(subDir);

            var image = TryLoadImage(imageFile);

            result.Add(new DashboardInfo
            {
                Id = $"{(isSystem ? "sys" : "usr")}_{dirName}",
                Name = metadata?.Name ?? dirName,
                Description = metadata?.Description ?? "",
                Image = image,
                IsSystem = isSystem,
                DirectoryPath = subDir,
            });
        }

        return result;
    }

    private static IImage? TryLoadImage(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            return new Bitmap(path);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load dashboard image: {Path}", path);
            return null;
        }
    }

    private static DashboardMetadata? LoadMetadata(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<DashboardMetadata>(json);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load dashboard metadata: {Path}", path);
            return null;
        }
    }

    private void EnsureSystemDashboards()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames()
                .Where(n => n.Contains(".Assets.Dashboards.ferrari_296_gt3"))
                .ToList();

            if (resourceNames.Count == 0)
            {
                Log.Warning("No embedded system dashboards found");
                return;
            }

            var sampleDir = Path.Combine(_systemDir, "Sample");
            Directory.CreateDirectory(sampleDir);

            foreach (var resourceName in resourceNames)
            {
                var parts = resourceName.Split('.');
                var fileName = parts[^2] + "." + parts[^1];

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream is null) continue;

                var targetPath = Path.Combine(sampleDir, fileName);
                using var fileStream = File.Create(targetPath);
                stream.CopyTo(fileStream);
            }

            Log.Information("Extracted system dashboards to {Path}", _systemDir);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to extract system dashboards");
        }
    }

    private void EnsureUserDirectory()
    {
        try
        {
            Directory.CreateDirectory(_userDir);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to create user dashboards directory");
        }
    }
}
