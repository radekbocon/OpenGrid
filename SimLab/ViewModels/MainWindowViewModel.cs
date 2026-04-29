using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using SimLab.Models;
using SimLab.Services;

namespace SimLab.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly ITelemetryService _telemetryService;

    public IReadOnlyList<SteamGame> SupportedGames { get; }
    
    [ObservableProperty]
    public partial SteamGame SelectedGame { get; set; }
    
    public ObservableCollection<MenuItem> MenuItems { get; }

    public ViewModelBase CurrentViewModel => _navigationService.CurrentViewModel;

    [ObservableProperty]
    public partial MenuItem SelectedItem { get; set; }
    
    [ObservableProperty]
    public partial string? TelemetryStatus { get; set; }

    [ObservableProperty] public partial string? ConnectLabel { get; set; } = "Connect";
    
    [ObservableProperty]
    public partial bool IsConnected { get; set; }

    public MainWindowViewModel(INavigationService navigationService,
        ITelemetryService telemetryService)
    {
        _navigationService = navigationService;
        _telemetryService = telemetryService;
        _telemetryService.TelemetryStatusChanged += TelemetryServiceOnTelemetryStatusChanged;
        _telemetryService.TelemetryReceived += TelemetryServiceOnTelemetryReceived;

        MenuItems =
        [
            new MenuItem { Icon = "🏠", Label = "Home", ViewModelType = typeof(HomeViewModel) },
            new MenuItem { Icon = "📋", Label = "Sessions", ViewModelType = typeof(SessionsViewModel) },
            new MenuItem { Icon = "⚙️", Label = "Settings", ViewModelType = typeof(SettingsViewModel) },
        ];
        SelectedItem = MenuItems.First();
        
        SupportedGames = SteamGame.GetAll();
        SelectedGame = SupportedGames.First();
    }

    private void TelemetryServiceOnTelemetryReceived(object? sender, TelemetryEventArgs e)
    {
    }

    private void TelemetryServiceOnTelemetryStatusChanged(object? sender, TelemetryConnectionStatus e)
    {
        IsConnected = e == TelemetryConnectionStatus.Connected;
        TelemetryStatus = e.ToString();
    }

    partial void OnSelectedItemChanged(MenuItem value)
    {
        _navigationService.NavigateTo(value.ViewModelType);
        OnPropertyChanged(nameof(CurrentViewModel));
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (IsConnected)
        {
            return;
        }

        Log.Information("Connecting to {0}", SelectedGame);
        await _telemetryService.ConnectAsync(SelectedGame, CancellationToken.None);
        _telemetryService.StartReading();
    }
    
    [RelayCommand]
    private void Disconnect()
    {
        _telemetryService.StopReading();
    }
}
