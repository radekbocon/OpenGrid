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

    private CancellationTokenSource? _cancellationTokenSource;

    public IReadOnlyList<SteamGame> SupportedGames { get; }
    
    [ObservableProperty]
    public partial SteamGame SelectedGame { get; set; }
    
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
    public partial string? TelemetryStatus { get; set; }

    [ObservableProperty] 
    public partial string? ConnectLabel { get; set; } = "Connect";
    
    [ObservableProperty]
    public partial bool IsConnected { get; set; }

    [ObservableProperty]
    public partial bool IsConnecting { get; private set; }
    
    [ObservableProperty]
    public partial MenuItem? SelectedMenuItem { get; set; }
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoBackCommand))]
    public partial bool CanGoBack { get; set; }
    
    public bool ShowConnectButton => !IsConnected && !IsConnecting;

    public MainWindowViewModel(INavigationService navigationService,
        ITelemetryService telemetryService)
    {
        _navigationService = navigationService;
        _telemetryService = telemetryService;
        _telemetryService.TelemetryStatusChanged += TelemetryServiceOnTelemetryStatusChanged;
        _telemetryService.TelemetryReceived += TelemetryServiceOnTelemetryReceived;

        MenuItems =
        [
            new MenuItem { Icon = "", Label = "Home", ViewModelType = typeof(HomeViewModel) },
            new MenuItem { Icon = "", Label = "Sessions", ViewModelType = typeof(SessionsViewModel) },
            new MenuItem { Icon = "", Label = "Dashboards", ViewModelType = typeof(DashboardsViewModel) },
            new MenuItem { Icon = "", Label = "Devices", ViewModelType = typeof(DevicesViewModel) },
            new MenuItem { Icon = "", Label = "Settings", ViewModelType = typeof(SettingsViewModel) },
        ];
        
        SupportedGames = SteamGame.GetAll();
        SelectedGame = SupportedGames.First();
    }

    private void TelemetryServiceOnTelemetryReceived(object? sender, TelemetryEventArgs e)
    {
    }

    private void TelemetryServiceOnTelemetryStatusChanged(object? sender, TelemetryConnectionStatus e)
    {
        IsConnected = e == TelemetryConnectionStatus.Connected;
        IsConnecting = e == TelemetryConnectionStatus.Connecting;
        OnPropertyChanged(nameof(ShowConnectButton));
        TelemetryStatus = e.ToString();
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
    private async Task ConnectAsync()
    {
        if (IsConnected)
        {
            return;
        }

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
        Log.Information("Connecting to {0}", SelectedGame);
        await _telemetryService.ConnectAsync(SelectedGame, _cancellationTokenSource.Token);
        _telemetryService.StartReading();
    }
    
    [RelayCommand]
    private void Disconnect()
    {
        _telemetryService.StopReading();
    }
    
    [RelayCommand]
    private void Cancel()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }
}
