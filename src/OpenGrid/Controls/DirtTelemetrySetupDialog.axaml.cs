using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DialogHostAvalonia;
using OpenGrid.Models;

namespace OpenGrid.Controls;

public partial class DirtTelemetrySetupDialog : UserControl
{
    public bool IsWindows => OperatingSystem.IsWindows();
    public bool IsLinux => OperatingSystem.IsLinux();
    public string WindowsPath { get; }
    public string LinuxPath { get; }

    public DirtTelemetrySetupDialog(SteamGame game)
    {
        WindowsPath = $@"Documents\My Games\{game.InstallDirectory}\hardware_settings_config.xml";
        LinuxPath = $"~/.steam/steam/steamapps/compatdata/{game.AppId}/pfx/drive_c/users/steamuser/Documents/My Games/{game.InstallDirectory}/hardware_settings_config.xml";
        InitializeComponent();
        DataContext = this;
    }

    private void CloseClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Close(null);
    }
}
