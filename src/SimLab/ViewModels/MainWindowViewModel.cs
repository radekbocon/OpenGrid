using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using SimLab.Services;

namespace SimLab.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly ITelemetryService _telemetryService;
    private readonly IGameService _gameService;

    public ObservableCollection<MenuItem> MenuItems { get; }

    public ViewModelBase? CurrentViewModel
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                SelectedMenuItem = MenuItems.FirstOrDefault(m => m.ViewModelType == value?.GetType()) ?? SelectedMenuItem;
            }
        }
    }
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoBackCommand))]
    public partial bool CanGoBack { get; set; }

    [ObservableProperty]
    public partial MenuItem? SelectedMenuItem { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "";

    [ObservableProperty]
    public partial bool ShowCancelButton { get; set; }
    
    [ObservableProperty]
    public partial bool IsConnected { get; set; }


    public MainWindowViewModel(INavigationService navigationService,
        ITelemetryService telemetryService,
        IGameService gameService)
    {
        _navigationService = navigationService;
        _telemetryService = telemetryService;
        _gameService = gameService;
        _telemetryService.TelemetryReceived += TelemetryServiceOnTelemetryReceived;
        _gameService.GameProcessChanged += GameServiceOnGameProcessChanged;

        MenuItems =
        [
            new MenuItem { Icon = MaterialIconKind.Home, Label = "Home", ViewModelType = typeof(HomeViewModel) },
            new MenuItem { Icon = MaterialIconKind.ChartLine, Label = "Sessions", ViewModelType = typeof(SessionsViewModel) },
            new MenuItem { Icon = MaterialIconKind.Gauge, Label = "Dashboards", ViewModelType = typeof(DashboardsViewModel) },
            new MenuItem { Icon = MaterialIconKind.Devices, Label = "Devices", ViewModelType = typeof(DevicesViewModel) },
            new MenuItem { Icon = MaterialIconKind.Cog, Label = "Settings", ViewModelType = typeof(SettingsViewModel) },
        ];
    }

    private void GameServiceOnGameProcessChanged(object? sender, GameProcessEventArgs e)
    {
        IsConnected = e.Status == GameProcessStatus.Connected;
        ShowCancelButton = e.Status is GameProcessStatus.Connecting or GameProcessStatus.StartingGame;
        var gameName = e.GameItem?.Game.Name ?? "";
        var appId = e.GameItem?.Game.AppId;

        StatusMessage = e switch
        {
            { Status: GameProcessStatus.Connecting } => $"Connecting to {gameName} ({appId})",
            { Status: GameProcessStatus.StartingGame } => $"Starting {gameName} ({appId})",
            { Status: GameProcessStatus.Connected } => $"Connected to {gameName} ({appId})",
            _ => "",
        };
    }

    private void TelemetryServiceOnTelemetryReceived(object? sender, TelemetryEventArgs e)
    {
    }

    [RelayCommand]
    private void SelectMenuItem(MenuItem menuItem)
    {
        _navigationService.NavigateTo(menuItem.ViewModelType);
    }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    [RelayCommand]
    private void Cancel()
    {
        _gameService.Cancel();
    }

    [RelayCommand]
    private void Disconnect()
    {
        _telemetryService.StopReading();
        IsConnected = false;
        StatusMessage = "";
    }
}
