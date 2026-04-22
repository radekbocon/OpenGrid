using System;
using SimLab.ViewModels;

namespace SimLab.Services;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        // Default to Home
        CurrentViewModel = _serviceProvider.GetService(typeof(HomeViewModel)) as ViewModelBase ?? new HomeViewModel();
    }

    public ViewModelBase CurrentViewModel { get; private set; }

    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
        CurrentViewModel = _serviceProvider.GetService(typeof(TViewModel)) as ViewModelBase ?? CurrentViewModel;
    }

    public void NavigateTo(Type type)
    {
        CurrentViewModel = _serviceProvider.GetService(type) as ViewModelBase ?? CurrentViewModel;
    }
}
