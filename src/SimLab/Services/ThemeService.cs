using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Serilog;
using SimLab.Models;

namespace SimLab.Services;

public sealed class ThemeService : IThemeService
{
    private static readonly string ThemesDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SimLab",
        "themes");

    private readonly List<ThemeInfo> _themes = [];
    private ResourceDictionary? _currentThemeDict;

    public IReadOnlyList<ThemeInfo> Themes => _themes;
    public ThemeInfo? CurrentTheme { get; private set; }

    public ThemeService()
    {
        InitializeThemes();
    }

    public void ApplyTheme(ThemeInfo theme)
    {
        if (Application.Current is null) return;

        ArgumentNullException.ThrowIfNull(theme);

        Application.Current.RequestedThemeVariant = theme.Variant;

        if (_currentThemeDict is not null)
        {
            Application.Current.Resources.MergedDictionaries.Remove(_currentThemeDict);
        }

        var dict = new ResourceDictionary();
        foreach (var kvp in theme.Colors)
        {
            var brushKey = kvp.Key;
            var colorKey = brushKey.EndsWith("Brush") ? brushKey.Replace("Brush", "Color") : null;
            var color = kvp.Value;

            dict[brushKey] = new SolidColorBrush(color);
            if (colorKey is not null)
            {
                dict[colorKey] = color;
            }
        }

        Application.Current.Resources.MergedDictionaries.Add(dict);
        _currentThemeDict = dict;
        CurrentTheme = theme;

        Log.Information("Applied theme: {ThemeName} ({Variant})", theme.Name, theme.Variant);
    }

    public ThemeInfo? GetThemeByName(string name)
    {
        return _themes.FirstOrDefault(t =>
            string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private void InitializeThemes()
    {
        try
        {
            LoadThemesFromEmbeddedResources();
            LoadCustomThemesFromDirectory();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to initialize themes");
        }

        if (_themes.Count == 0)
        {
            Log.Warning("No themes found, creating fallback dark theme");
            _themes.Add(new ThemeInfo
            {
                Name = "Dark",
                Variant = ThemeVariant.Dark,
                Colors = new Dictionary<string, Color>(),
            });
        }
    }

    private void LoadThemesFromEmbeddedResources()
    {
        var assembly = Assembly.GetExecutingAssembly();
        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.Contains(".Default.") || !resourceName.EndsWith(".json"))
            {
                continue;
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                continue;
            }

            var theme = LoadThemeFromStream(stream);
            if (theme is not null)
            {
                _themes.Add(theme);
            }
        }
    }

    private void LoadCustomThemesFromDirectory()
    {
        Directory.CreateDirectory(ThemesDirectory);
        foreach (var jsonFile in Directory.GetFiles(ThemesDirectory, "*.json"))
        {
            try
            {
                using var stream = File.OpenRead(jsonFile);
                var theme = LoadThemeFromStream(stream);
                if (theme is not null)
                {
                    _themes.Add(theme);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to load custom theme from {File}", jsonFile);
            }
        }
    }

    private static ThemeInfo? LoadThemeFromStream(Stream stream)
    {
        try
        {
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var name = root.GetProperty("name").GetString() ?? "Unknown";
            var variantStr = root.GetProperty("variant").GetString();
            var variant = variantStr switch
            {
                "Light" => ThemeVariant.Light,
                _ => ThemeVariant.Dark,
            };

            var colors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
            var colorsElement = root.GetProperty("colors");
            foreach (var colorProp in colorsElement.EnumerateObject())
            {
                var hex = colorProp.Value.GetString();
                if (hex is null)
                {
                    continue;
                }

                try
                {
                    colors[colorProp.Name] = Color.Parse(hex);
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Failed to parse color '{Value}' for key '{Key}'", hex, colorProp.Name);
                }
            }

            return new ThemeInfo
            {
                Name = name,
                Variant = variant,
                Colors = colors,
            };
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load theme from embedded resource");
            return null;
        }
    }
}
