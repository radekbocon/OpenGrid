using System;
using System.Diagnostics;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DialogHostAvalonia;

namespace OpenGrid.Controls;

public partial class AboutDialog : UserControl
{
    public AboutDialog()
    {
        InitializeComponent();
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        if (version != null)
        {
            VersionText.Text = version.ToString();
        }

        CopyrightTextBlock.Text = $"© {DateTime.Now.Year} Radoslaw Bocon";
    }

    private void CloseClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Close(null);
    }

    private void ReportIssueClick(object? sender, RoutedEventArgs e)
    {
        Launcher.LaunchUriAsync("https://github.com/radekbocon/OpenGrid/issues").FireAndForgetSafe();
    }
}
