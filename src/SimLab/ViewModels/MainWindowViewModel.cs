using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using Material.Icons;
using SimLab.Controls;
using SimLab.Models;
using SimLab.Services;
using SimLab.Services.Telemetry;

namespace SimLab.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly ITelemetryService _telemetryService;
    private readonly SessionRepository _sessionRepository;

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
    [NotifyPropertyChangedFor(nameof(CanStartRecording))]
    [NotifyPropertyChangedFor(nameof(CanStopRecording))]
    public partial TelemetryConnectionStatus ConnectionStatus { get; set; }

    [ObservableProperty]
    public partial string TelemetryStatusText { get; set; } = "Not connected";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStartRecording))]
    [NotifyPropertyChangedFor(nameof(CanStopRecording))]
    public partial bool IsRecording { get; set; }

    public bool CanStartRecording => ConnectionStatus == TelemetryConnectionStatus.Connected && !IsRecording;
    public bool CanStopRecording => IsRecording;

    public MainWindowViewModel(INavigationService navigationService,
        ITelemetryService telemetryService,
        SessionRepository sessionRepository)
    {
        _navigationService = navigationService;
        _telemetryService = telemetryService;
        _sessionRepository = sessionRepository;
        _sessionRepository.IsRecordingChanged += (_, args) => IsRecording = args;
        _telemetryService.TelemetryReceived += TelemetryServiceOnTelemetryReceived;
        _telemetryService.TelemetryStatusChanged += OnTelemetryStatusChanged;

        ConnectionStatus = _telemetryService.ConnectionStatus;
        OnTelemetryStatusChanged(_telemetryService, _telemetryService.ConnectionStatus);

        MenuItems =
        [
            new MenuItem { Icon = MaterialIconKind.Home, Label = "Home", ViewModelType = typeof(HomeViewModel) },
            new MenuItem { Icon = MaterialIconKind.ChartLine, Label = "Sessions", ViewModelType = typeof(SessionsViewModel) },
            new MenuItem { Icon = MaterialIconKind.Gauge, Label = "Dashboards", ViewModelType = typeof(DashboardsViewModel) },
            new MenuItem { Icon = MaterialIconKind.Devices, Label = "Devices", ViewModelType = typeof(DevicesViewModel) },
            new MenuItem { Icon = MaterialIconKind.Cog, Label = "Settings", ViewModelType = typeof(SettingsViewModel) },
        ];
    }

    private void OnTelemetryStatusChanged(object? sender, TelemetryConnectionStatus status)
    {
        ConnectionStatus = status;
        var gameName = _telemetryService.CurrentGame?.Name;
        TelemetryStatusText = status switch
        {
            TelemetryConnectionStatus.Connecting => $"Connecting to {gameName}",
            TelemetryConnectionStatus.Connected => $"Connected to {gameName}",
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
    private async Task StartRecordingAsync()
    {
        await _sessionRepository.StartRecordingAsync();
        IsRecording = true;
    }

    [RelayCommand]
    private async Task StopRecordingAsync()
    {
        await _sessionRepository.StopRecordingAsync();
        IsRecording = false;
    }

    [RelayCommand]
    private void Disconnect()
    {
        if (IsRecording)
        {
            _ = _sessionRepository.StopRecordingAsync();
            IsRecording = false;
        }
        _telemetryService.StopReading();
    }
    
    [RelayCommand]
    private void About()
    {
        var dialog = new AboutDialog();
        DialogHost.Show(dialog);
    }
    
    [RelayCommand]
    private async Task ProjectPageAsync()
    {
        await Launcher.LaunchUriAsync("https://github.com/radekbocon/SimLab");
    }

    [RelayCommand]
    private async Task ShowLogsFolderAsync()
    {
        var logsFolder = Path.Combine(Program.AppDataDirectory, "logs");
        if (Directory.Exists(logsFolder))
        {
            await Launcher.LaunchDirectoryAsync(logsFolder);
        }
    }
}
