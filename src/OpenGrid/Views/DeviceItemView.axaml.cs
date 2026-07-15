using Avalonia;
using Avalonia.Controls;
using OpenGrid.Models;
using OpenGrid.ViewModels;

namespace OpenGrid.Views;

public partial class DeviceItemView : UserControl
{
    private static readonly DashboardInfo NoneDashboard = new() { Id = "", Name = "None", Description = "", DirectoryPath = "" };

    public static readonly StyledProperty<DeviceItemViewModel?> DeviceProperty =
        AvaloniaProperty.Register<DeviceItemView, DeviceItemViewModel?>(nameof(Device));

    public DeviceItemViewModel? Device
    {
        get => GetValue(DeviceProperty);
        set => SetValue(DeviceProperty, value);
    }

    public DeviceItemView()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DeviceProperty)
        {
            PopulateDashboardComboBox();
        }
    }

    private void PopulateDashboardComboBox()
    {
        if (DashboardComboBox is null || Device is null)
            return;

        var items = new List<DashboardInfo> { NoneDashboard };
        items.AddRange(Device.AvailableDashboards);
        DashboardComboBox.ItemsSource = items;

        if (!string.IsNullOrEmpty(Device.SelectedDashboard?.Id))
        {
            for (var i = 0; i < Device.AvailableDashboards.Count; i++)
            {
                if (string.Equals(Device.AvailableDashboards[i].Id, Device.SelectedDashboard.Id, System.StringComparison.Ordinal))
                {
                    DashboardComboBox.SelectedIndex = i + 1;
                    return;
                }
            }
        }

        DashboardComboBox.SelectedIndex = 0;
    }

    private void DashboardComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Device is null)
            return;

        if (DashboardComboBox.SelectedItem is DashboardInfo selected)
        {
            Device.SelectedDashboard = selected.Id == NoneDashboard.Id ? null : selected;
        }
        else if (DashboardComboBox.SelectedIndex == -1)
        {
            Device.SelectedDashboard = null;
        }
    }
}
