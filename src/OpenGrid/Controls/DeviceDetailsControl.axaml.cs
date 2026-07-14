using Avalonia;
using Avalonia.Controls;
using OpenGrid.ViewModels;

namespace OpenGrid.Controls;

public partial class DeviceDetailsControl : UserControl
{
    public static readonly StyledProperty<DeviceItemViewModel?> DeviceProperty =
        AvaloniaProperty.Register<DeviceDetailsControl, DeviceItemViewModel?>(nameof(Device));

    public DeviceItemViewModel? Device
    {
        get => GetValue(DeviceProperty);
        set => SetValue(DeviceProperty, value);
    }

    public DeviceDetailsControl()
    {
        InitializeComponent();
    }
}
