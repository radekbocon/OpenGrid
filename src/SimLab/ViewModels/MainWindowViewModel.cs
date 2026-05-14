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

    internal event Action? CancelRequested;

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
    private string? _telemetryStatus;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCancelButton))]
    [NotifyPropertyChangedFor(nameof(StatusMessage))]
    private bool _isConnected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCancelButton))]
    [NotifyPropertyChangedFor(nameof(StatusMessage))]
    private bool _isConnecting;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCancelButton))]
    [NotifyPropertyChangedFor(nameof(StatusMessage))]
    private bool _isWaitingForGame;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoBackCommand))]
    public partial bool CanGoBack { get; set; }

    [ObservableProperty]
    public partial MenuItem? SelectedMenuItem { get; set; }

    public bool ShowCancelButton => IsWaitingForGame || IsConnecting;

    public string StatusMessage => (IsWaitingForGame, IsConnecting, IsConnected) switch
    {
        (true, _, _) => "Launching game...",
        (_, true, _) => "Connecting...",
        (_, _, true) => "Connected",
        _ => TelemetryStatus ?? "None"
    };

    public MainWindowViewModel(INavigationService navigationService,
        ITelemetryService telemetryService,
        ISettingsService settingsService)
    {
        _navigationService = navigationService;
        _telemetryService = telemetryService;
        _telemetryService.TelemetryStatusChanged += TelemetryServiceOnTelemetryStatusChanged;
        _telemetryService.TelemetryReceived += TelemetryServiceOnTelemetryReceived;

        MenuItems =
        [
            new MenuItem { Icon = MaterialIconKind.Home, Label = "Home", ViewModelType = typeof(HomeViewModel) },
            new MenuItem { Icon = MaterialIconKind.ChartLine, Label = "Sessions", ViewModelType = typeof(SessionsViewModel) },
            new MenuItem { Icon = MaterialIconKind.Gauge, Label = "Dashboards", ViewModelType = typeof(DashboardsViewModel) },
            new MenuItem { Icon = MaterialIconKind.Devices, Label = "Devices", ViewModelType = typeof(DevicesViewModel) },
            new MenuItem { Icon = MaterialIconKind.Cog, Label = "Settings", ViewModelType = typeof(SettingsViewModel) },
        ];
    }

    private void TelemetryServiceOnTelemetryReceived(object? sender, TelemetryEventArgs e)
    {
    }

    private void TelemetryServiceOnTelemetryStatusChanged(object? sender, TelemetryConnectionStatus e)
    {
        TelemetryStatus = e.ToString();
        IsConnected = e == TelemetryConnectionStatus.Connected;
        IsConnecting = e == TelemetryConnectionStatus.Connecting;
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
        CancelRequested?.Invoke();
        IsWaitingForGame = false;
    }

    [RelayCommand]
    private void Disconnect()
    {
        _telemetryService.StopReading();
    }
}
