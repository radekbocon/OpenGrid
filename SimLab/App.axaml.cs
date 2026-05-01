using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using SimLab.ViewModels;
using SimLab.Views;
using Microsoft.Extensions.DependencyInjection;
using SimLab.Services;

namespace SimLab;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainVewModel = Program.ServiceProvider.GetRequiredService<MainWindowViewModel>();
            var navigationService = Program.ServiceProvider.GetRequiredService<INavigationService>();
            navigationService.Initialize(mainVewModel);
            navigationService.NavigateTo<HomeViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainVewModel,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}