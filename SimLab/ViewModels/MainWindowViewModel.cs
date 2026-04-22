using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimLab.Services;

namespace SimLab.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;

    public MainWindowViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;

        MenuItems =
        [
            new ManuItem { Icon = "🏠", Label = "Home", ViewModelType = typeof(HomeViewModel) },
            new ManuItem { Icon = "📋", Label = "Sessions", ViewModelType = typeof(SessionsViewModel) },
            new ManuItem { Icon = "⚙️", Label = "Settings", ViewModelType = typeof(SettingsViewModel) },
        ];
        SelectedItem = MenuItems[0];
    }

    public ObservableCollection<ManuItem> MenuItems { get; }

    public ViewModelBase CurrentViewModel => _navigationService.CurrentViewModel;

    [ObservableProperty]
    public partial ManuItem SelectedItem { get; set; }

    partial void OnSelectedItemChanged(ManuItem value)
    {
        _navigationService.NavigateTo(value.ViewModelType);
        OnPropertyChanged(nameof(CurrentViewModel));
    }

    [RelayCommand]
    private void Connect()
    {
        // TODO: Implement connect logic
    }
}
