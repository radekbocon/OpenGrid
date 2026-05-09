using System.Collections.Generic;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using SimLab.Services;

namespace SimLab.ViewModels;

public enum AppTheme { System, Light, Dark }

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;

    public IReadOnlyList<AppTheme> Themes { get; } = [AppTheme.System, AppTheme.Light, AppTheme.Dark];

    [ObservableProperty]
    public partial AppTheme SelectedTheme { get; set; } = AppTheme.System;
    
    public bool MinimizeToTray
    {
        get => _settingsService.MinimizeToTray;
        set => _settingsService.MinimizeToTray = value;
    }

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        
        IsMenuItem = true;
    }

    partial void OnSelectedThemeChanged(AppTheme value)
    {
        if (Application.Current is null) return;
        Application.Current.RequestedThemeVariant = value switch
        {
            AppTheme.Light => ThemeVariant.Light,
            AppTheme.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
    }
}
