using System.Collections.Generic;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SimLab.ViewModels;

public enum AppTheme { System, Light, Dark }

public partial class SettingsViewModel : ViewModelBase
{
    public IReadOnlyList<AppTheme> Themes { get; } = [AppTheme.System, AppTheme.Light, AppTheme.Dark];

    [ObservableProperty]
    private AppTheme _selectedTheme = AppTheme.System;

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
