using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SimLab.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog(string message)
    {
        InitializeComponent();
        MessageText.Text = message;
    }

    private void YesClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void NoClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
