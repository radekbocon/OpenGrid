using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace OpenGrid.Services;

internal record UserSettings
{
    public bool MinimizeToTray { get; set; } = true;
    public int? DashboardPort { get; set; }
    public string? SelectedTheme { get; set; }
    public int RecordingRateHz { get; set; } = 30;
    public HashSet<int> AutoConnectGameAppIds { get; set; } = [];
    public HashSet<int> AutoRecordingGameAppIds { get; set; } = [];
}

public sealed class SettingsService : ISettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Program.AppDataDirectory,
        "settings.json");
    
    private UserSettings _userSettings = new()
    {
        MinimizeToTray = true
    };

    public bool MinimizeToTray
    {
        get => _userSettings.MinimizeToTray;
        set
        {
            _userSettings.MinimizeToTray = value;
            Save();
        }
    }

    public string? SelectedTheme
    {
        get => _userSettings.SelectedTheme;
        set
        {
            _userSettings.SelectedTheme = value;
            Save();
        }
    }

    public int RecordingRateHz
    {
        get => _userSettings.RecordingRateHz;
        set
        {
            _userSettings.RecordingRateHz = value;
            Save();
        }
    }

    public HashSet<int> AutoConnectGameAppIds
    {
        get => _userSettings.AutoConnectGameAppIds;
        set
        {
            _userSettings.AutoConnectGameAppIds = value;
            Save();
        }
    }

    public HashSet<int> AutoRecordingGameAppIds
    {
        get => _userSettings.AutoRecordingGameAppIds;
        set
        {
            _userSettings.AutoRecordingGameAppIds = value;
            Save();
        }
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath);
            if (dir is not null)
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(_userSettings));
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Failed to save settings");
        }
    }

    public void Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return;
            }
            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<UserSettings>(json);
            _userSettings = settings ?? _userSettings;
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Failed to load settings");
        }
    }
}
