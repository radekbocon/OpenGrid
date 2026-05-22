using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using SimLab.ViewModels;
using SimLab.Views;
using Microsoft.Extensions.DependencyInjection;
using SimLab.Services;
using SimLab.Services.Telemetry;

namespace SimLab;

public class App : Application
{
    private TrayIcon? _trayIcon;
    private ISettingsService? _settingsService;
    private IThemeService? _themeService;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainVewModel = Program.ServiceProvider.GetRequiredService<MainWindowViewModel>();
            var navigationService = Program.ServiceProvider.GetRequiredService<INavigationService>();
            _settingsService = Program.ServiceProvider.GetRequiredService<ISettingsService>();
            _settingsService.Load();
            _themeService = Program.ServiceProvider.GetRequiredService<IThemeService>();
            ApplySavedTheme();
            navigationService.Initialize(mainVewModel);
            navigationService.NavigateTo<HomeViewModel>();
            var mainWindow = new MainWindow
            {
                DataContext = mainVewModel
            };
            desktop.MainWindow = mainWindow;

            mainWindow.Closing += MainWindowOnClosing;
            desktop.Exit += DesktopOnExit;
            

            using var iconStream = AssetLoader.Open(new Uri("avares://SimLab/Assets/simlab_icon.ico"));
            _trayIcon = new TrayIcon
            {
                Icon = new WindowIcon(iconStream),
                ToolTipText = "SimLab",
                Menu = new NativeMenu()
            };

            var showItem = new NativeMenuItem("Show SimLab");
            showItem.Click += (_, _) => ShowMainWindow(desktop);

            var exitItem = new NativeMenuItem("Exit");
            exitItem.Click += (_, _) =>
            {
                _trayIcon?.Dispose();
                _trayIcon = null;
                desktop.Shutdown();
            };

            _trayIcon.Menu.Items.Add(showItem);
            _trayIcon.Menu.Items.Add(new NativeMenuItemSeparator());
            _trayIcon.Menu.Items.Add(exitItem);

            _trayIcon.Clicked += (_, _) => ShowMainWindow(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void MainWindowOnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_settingsService?.MinimizeToTray == true && sender is MainWindow mainWindow)
        {
            e.Cancel = true;
            mainWindow.Hide();
        }
    }

    private void DesktopOnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        try
        {
            var telemetryService = Program.ServiceProvider.GetService<ITelemetryService>();
            telemetryService?.Dispose();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error during application exit cleanup");
        }
    }

    private void ApplySavedTheme()
    {
        var themeName = _settingsService?.SelectedTheme;
        var theme = !string.IsNullOrEmpty(themeName)
            ? _themeService?.GetThemeByName(themeName)
            : null;
        theme ??= _themeService?.Themes.FirstOrDefault(t =>
            string.Equals(t.Name, "Dracula", StringComparison.OrdinalIgnoreCase));
        theme ??= _themeService?.Themes.FirstOrDefault();
        if (theme is not null)
        {
            _themeService?.ApplyTheme(theme);
        }
    }

    private static void ShowMainWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (desktop.MainWindow is null)
        {
            return;
        }

        desktop.MainWindow.Show();
        desktop.MainWindow.WindowState = WindowState.Normal;
        desktop.MainWindow.Activate();
    }
}
