using System.Security;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SimLab.ViewModels;

namespace SimLab.Views;

public partial class DashboardWindow : Window
{
    public DashboardWindow(DashboardStyle style)
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
        PointerEntered += (_, _) => CloseButton.IsVisible = true;
        PointerExited += (_, _) => CloseButton.IsVisible = false;
        
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

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}

public enum DashboardStyle
{
    Default,
    Overlay
}
