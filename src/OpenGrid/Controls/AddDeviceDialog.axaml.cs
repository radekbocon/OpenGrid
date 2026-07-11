using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DialogHostAvalonia;
using OpenGrid.Models;

namespace OpenGrid.Controls;

public partial class AddDeviceDialog : UserControl
{
    public DeviceInfo? SelectedDevice { get; private set; }

    public List<DeviceInfo> AvailableDevices { get; }

    public AddDeviceDialog(List<DeviceInfo> devices)
    {
        InitializeComponent();
        AvailableDevices = devices;
        DataContext = this;

        DevicesList.SelectionChanged += (_, _) =>
        {
            AddButton.IsEnabled = DevicesList.SelectedItem is not null;
        };
    }

    private void AddClick(object? sender, RoutedEventArgs e)
    {
        SelectedDevice = DevicesList.SelectedItem as DeviceInfo;
        DialogHost.Close(null);
    }

    private void CancelClick(object? sender, RoutedEventArgs e)
    {
        SelectedDevice = null;
        DialogHost.Close(null);
    }

    private void CloseClick(object? sender, RoutedEventArgs e)
    {
        SelectedDevice = null;
        DialogHost.Close(null);
    }
}
