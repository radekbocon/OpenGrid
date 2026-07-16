using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using Material.Icons;
using OpenGrid.Controls;
using OpenGrid.Models;
using OpenGrid.Services;
using OpenGrid.Services.SessionPersist;
using OpenGrid.Services.Telemetry;

namespace OpenGrid.ViewModels;

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
    public partial MenuItem? SelectedMenuItem { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStartRecording))]
    [NotifyPropertyChangedFor(nameof(CanStopRecording))]
    private partial TelemetryConnectionStatus ConnectionStatus { get; set; }

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
            new MenuItem { Icon = MaterialIconKind.Car, Label = "Cars", ViewModelType = typeof(CarsViewModel) },
            new MenuItem { Icon = MaterialIconKind.Devices, Label = "Devices", ViewModelType = typeof(DevicesViewModel) },
            new MenuItem { Icon = MaterialIconKind.Cog, Label = "Settings", ViewModelType = typeof(SettingsViewModel) },
        ];
    }

    private void OnTelemetryStatusChanged(object? sender, TelemetryConnectionStatus status)
    {
        ConnectionStatus = status;
    }

    private void TelemetryServiceOnTelemetryReceived(object? sender, TelemetryEventArgs e)
    {
    }

    [RelayCommand]
    private void SelectMenuItem(MenuItem menuItem)
    {
        _navigationService.NavigateTo(menuItem.ViewModelType);
    }

    [RelayCommand]
    private async Task StartRecordingAsync()
    {
        _sessionRepository.StartRecording();
        IsRecording = true;
    }

    [RelayCommand]
    private void StopRecording()
    {
        _sessionRepository.StopRecording();
        IsRecording = false;
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
        await Launcher.LaunchUriAsync("https://github.com/radekbocon/OpenGrid");
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
