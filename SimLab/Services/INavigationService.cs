using System;
using SimLab.ViewModels;

namespace SimLab.Services;

public interface INavigationService
{
    ViewModelBase CurrentViewModel { get; }
    void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;
    void NavigateTo(Type type);
}
