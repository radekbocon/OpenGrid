using System;
using SimLab.ViewModels;

namespace SimLab.Services;

public interface INavigationService
{
    bool CanGoBack { get; }
    void Initialize(MainWindowViewModel mainWindowViewModel);
    void NavigateTo<TViewModel>(params object[]  parameters) where TViewModel : ViewModelBase;
    void NavigateTo(Type type);
    void GoBack();
}
