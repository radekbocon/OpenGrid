using Avalonia.Controls;
using Avalonia.Interactivity;
using DialogHostAvalonia;

namespace SimLab.Controls;

public partial class AcTelemetrySetupDialog : UserControl
{
    public AcTelemetrySetupDialog()
    {
        InitializeComponent();
    }

    private void CloseClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Close(null);
    }
}
