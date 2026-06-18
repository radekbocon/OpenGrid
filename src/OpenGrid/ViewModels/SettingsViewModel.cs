using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenGrid.Models;
using OpenGrid.Services;

namespace OpenGrid.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IThemeService _themeService;

    public IReadOnlyList<ThemeInfo> Themes => _themeService.Themes;

    [ObservableProperty]
    public partial ThemeInfo? SelectedTheme { get; set; }

    public bool MinimizeToTray
    {
        get => _settingsService.MinimizeToTray;
        set => _settingsService.MinimizeToTray = value;
    }

    public int RecordingRateHz
    {
        get => _settingsService.RecordingRateHz;
        set => _settingsService.RecordingRateHz = value;
    }

    public SettingsViewModel(ISettingsService settingsService, IThemeService themeService)
    {
        _settingsService = settingsService;
        _themeService = themeService;

        IsMenuItem = true;

        SelectedTheme = _themeService.CurrentTheme;
    }

    partial void OnSelectedThemeChanged(ThemeInfo? value)
    {
        if (value is null) return;
        _themeService.ApplyTheme(value);
        _settingsService.SelectedTheme = value.Name;
    }
}
