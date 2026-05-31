using System.Diagnostics;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DialogHostAvalonia;

namespace SimLab.Controls;

public partial class AboutDialog : UserControl
{
    public AboutDialog()
    {
        InitializeComponent();
        var version = Assembly.GetEntryAssembly()?.GetName()?.Version;
        if (version != null)
        {
            VersionText.Text = $"Version {version.Major}.{version.Minor}.{version.Build}";
        }
    }

    private void CloseClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Close(null);
    }

    private void ReportIssueClick(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/radekbocon/SimLab/issues",
            UseShellExecute = true
        });
    }
}
