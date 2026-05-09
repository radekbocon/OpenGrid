using System.Security;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SimLab.ViewModels;

namespace SimLab.Views;

public partial class DashboardWindow : Window
{
    public DashboardWindow()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
    }

    public void Initialize(DashboardStyle style)
    {
        if (style == DashboardStyle.Overlay)
        {
            Background = null;
            TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }

    private void Fullscreen(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;
    }

    private void Close(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}

public enum DashboardStyle
{
    Default,
    Overlay
}
