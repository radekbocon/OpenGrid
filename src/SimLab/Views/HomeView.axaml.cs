using Avalonia.Controls;
using Avalonia.Interactivity;
using SimLab.ViewModels;

namespace SimLab.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (DataContext is HomeViewModel vm)
        {
            vm.OnActivated();
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        if (DataContext is HomeViewModel vm)
        {
            vm.OnDeactivated();
        }
    }
}
