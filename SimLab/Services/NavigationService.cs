using System;
using System.Collections.Generic;
using SimLab.ViewModels;

namespace SimLab.Services;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Stack<ViewModelBase> _viewModels = new();
    
    private MainWindowViewModel? _mainWindowViewModel;
    
    public bool CanGoBack => _viewModels.Count > 1;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Initialize(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;
    }

    public void NavigateTo<TViewModel>(params object[]  parameters) where TViewModel : ViewModelBase
    {
        var viewModel = _serviceProvider.GetService(typeof(TViewModel)) as TViewModel ?? throw new InvalidOperationException($"No view model of type {typeof(TViewModel)} found");

        if (parameters.Length > 0)
        {
            viewModel.SetParameters(parameters);
        }
        
        _viewModels.Push(viewModel);
        
        _mainWindowViewModel?.CurrentViewModel = viewModel;
        _mainWindowViewModel?.CanGoBack = CanGoBack;
    }

    public void NavigateTo(Type type)
    {
        var viewModel = _serviceProvider.GetService(type) as ViewModelBase ?? throw new InvalidOperationException($"No view model of type {type} found");
        
        _viewModels.Push(viewModel);
        
        _mainWindowViewModel?.CurrentViewModel = viewModel;
        _mainWindowViewModel?.CanGoBack = CanGoBack;
    }
    
    public void GoBack()
    {
        if (_viewModels.Count > 1)
        {
            _viewModels.Pop();
            _mainWindowViewModel?.CurrentViewModel = _viewModels.Peek();
            _mainWindowViewModel?.CanGoBack = CanGoBack;
        }
    }
}
