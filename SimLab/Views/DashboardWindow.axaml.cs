using System.Security;
using Avalonia.Controls;
using Avalonia.Input;
using SimLab.ViewModels;

namespace SimLab.Views;

public partial class DashboardWindow : Window
{
    public DashboardWindow(DashboardStyle style)
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
        
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
}

public enum DashboardStyle
{
    Default,
    Overlay
}
