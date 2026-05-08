using Avalonia.Controls;
using Avalonia.Input;
using SimLab.Models;

namespace SimLab.Views;

public partial class DashboardsView : UserControl
{
    public DashboardsView()
    {
        InitializeComponent();
    }

    private void OnDashboardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border { DataContext: DashboardInfo dashboard } border &&
            DataContext is ViewModels.DashboardsViewModel vm)
        {
            vm.OpenDashboardCommand.Execute(dashboard);
        }
    }
}
