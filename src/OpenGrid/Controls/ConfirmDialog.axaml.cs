using Avalonia.Controls;
using Avalonia.Interactivity;
using DialogHostAvalonia;

namespace OpenGrid.Controls;

public partial class ConfirmDialog : UserControl
{
    public bool Result { get; private set; }
    
    public ConfirmDialog(string message)
    {
        InitializeComponent();
        MessageText.Text = message;
    }

    private void YesClick(object? sender, RoutedEventArgs e)
    {
        Result = true;
        DialogHost.Close(null);
    }

    private void NoClick(object? sender, RoutedEventArgs e)
    {
        Result = false;   
        DialogHost.Close(null);
    }
}
