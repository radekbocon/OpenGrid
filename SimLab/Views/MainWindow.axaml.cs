using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SimLab.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void ToggleSideBar(object? sender, RoutedEventArgs e)
    {
        const double open = 220;
        const double closed = 45;

        SideBar.Width = SideBar.Width < open ? open : closed;
    }
}