using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DialogHostAvalonia;
using SimLab.Models;

namespace SimLab.Controls;

public partial class DirtTelemetrySetupDialog : UserControl
{
    public bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    public string GameName { get; }
    public string WindowsPath { get; }
    public string LinuxPath { get; }

    public DirtTelemetrySetupDialog(SteamGame game)
    {
        GameName = game.Name;
        WindowsPath = $@"Documents\My Games\{game.InstallDirectory}\hardware_settings_config.xml";
        LinuxPath = $@"~/.steam/steam/steamapps/compatdata/{game.AppId}/pfx/drive_c/users/steamuser/Documents/My Games/{game.InstallDirectory}/hardware_settings_config.xml";
        InitializeComponent();
        DataContext = this;
    }

    private void CloseClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Close(null);
    }
}
