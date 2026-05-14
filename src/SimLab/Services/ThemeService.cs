using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
    private ThemeInfo? _currentTheme;
    private ResourceDictionary? _currentThemeDict;

    public IReadOnlyList<ThemeInfo> Themes => _themes;
    public ThemeInfo? CurrentTheme => _currentTheme;

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
        _currentTheme = theme;

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
            Directory.CreateDirectory(ThemesDirectory);
            ExtractDefaultThemes();
            LoadThemesFromDirectory();
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

    private void ExtractDefaultThemes()
    {
        var existingFiles = Directory.GetFiles(ThemesDirectory, "*.css");
        if (existingFiles.Length > 0)
            return;

        var assembly = Assembly.GetExecutingAssembly();
        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.Contains(".Default.") || !resourceName.EndsWith(".css"))
                continue;

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null) continue;

            var parts = resourceName.Split('.');
            var fileName = string.Join(".", parts[^2], parts[^1]);
            var filePath = Path.Combine(ThemesDirectory, fileName);

            using var fileStream = File.Create(filePath);
            stream.CopyTo(fileStream);
        }
    }

    private void LoadThemesFromDirectory()
    {
        foreach (var cssFile in Directory.GetFiles(ThemesDirectory, "*.css"))
        {
            var theme = LoadThemeFromFile(cssFile);
            if (theme is not null)
                _themes.Add(theme);
        }
    }

    private static ThemeInfo? LoadThemeFromFile(string filePath)
    {
        try
        {
            var css = File.ReadAllText(filePath);
            var colors = ParseCssColors(css);
            if (colors.Count == 0) return null;

            var themeVariantValue = ExtractRawCssVariable(css, "--ThemeVariant");

            var variant = themeVariantValue switch
            {
                "Light" => ThemeVariant.Light,
                _ => ThemeVariant.Dark,
            };

            var displayName = themeVariantValue ?? Path.GetFileNameWithoutExtension(filePath);

            var brushColors = colors
                .Where(kvp => kvp.Key.StartsWith("Material"))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return new ThemeInfo
            {
                Name = displayName,
                FilePath = filePath,
                Variant = variant,
                Colors = brushColors,
            };
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load theme from {File}", filePath);
            return null;
        }
    }

    private static string? ExtractRawCssVariable(string css, string variableName)
    {
        var prefix = variableName + ":";
        foreach (var line in css.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var value = trimmed[prefix.Length..].Trim().TrimEnd(';').Trim();
            return string.IsNullOrEmpty(value) ? null : value;
        }
        return null;
    }

    private static Dictionary<string, Color> ParseCssColors(string css)
    {
        var colors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
        var lines = css.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("/*") || trimmed.StartsWith(":root") ||
                trimmed.StartsWith("}") || trimmed.StartsWith("{"))
                continue;

            var colonIdx = trimmed.IndexOf(':');
            if (colonIdx < 0) continue;

            var key = trimmed[..colonIdx].Trim();
            if (!key.StartsWith("--")) continue;

            var valuePart = trimmed[(colonIdx + 1)..].Trim().TrimEnd(';').Trim();
            if (string.IsNullOrEmpty(valuePart)) continue;

            try
            {
                var color = Color.Parse(valuePart);// ParseCssColor(valuePart);
                var cleanKey = key[2..];
                colors[cleanKey] = color;
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Failed to parse color '{Value}' for key '{Key}'", valuePart, key);
            }
        }

        return colors;
    }
}
