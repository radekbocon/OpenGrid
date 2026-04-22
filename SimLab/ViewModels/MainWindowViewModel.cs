using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimLab.Models;
using SimLab.Services;

namespace SimLab.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly SharedMemoryBridgeLauncher _sharedMemoryBridgeLauncher;
    private readonly SharedFileReader _sharedFileReader;

    public IReadOnlyList<SteamGame> SupportedGames { get; }
    
    [ObservableProperty]
    public partial SteamGame SelectedGame { get; set; }
    
    public ObservableCollection<ManuItem> MenuItems { get; }

    public ViewModelBase CurrentViewModel => _navigationService.CurrentViewModel;

    [ObservableProperty]
    public partial ManuItem SelectedItem { get; set; }

    [ObservableProperty]
    public partial string BridgeConnectionStatus { get; set; }
    
    [ObservableProperty]
    public partial string? TelemetryStatus { get; set; }

    public MainWindowViewModel(INavigationService navigationService,
        SharedMemoryBridgeLauncher sharedMemoryBridgeLauncher,
        SharedFileReader sharedFileReader)
    {
        _navigationService = navigationService;
        _sharedMemoryBridgeLauncher = sharedMemoryBridgeLauncher;
        _sharedFileReader = sharedFileReader;
        _sharedMemoryBridgeLauncher.ConnectionStatusChanged += SharedMemoryBridgeLauncherOnConnectionStatusChanged;
        _sharedFileReader.ConnectionStatusChanged += SharedFileReaderOnConnectionStatusChanged;

        BridgeConnectionStatus = _sharedMemoryBridgeLauncher.ConnectionStatus;
        MenuItems =
        [
            new ManuItem { Icon = "🏠", Label = "Home", ViewModelType = typeof(HomeViewModel) },
            new ManuItem { Icon = "📋", Label = "Sessions", ViewModelType = typeof(SessionsViewModel) },
            new ManuItem { Icon = "⚙️", Label = "Settings", ViewModelType = typeof(SettingsViewModel) },
        ];
        SelectedItem = MenuItems.First();
        
        SupportedGames = SteamGame.GetAll();
        SelectedGame = SupportedGames.First();
    }

    private void SharedFileReaderOnConnectionStatusChanged(object? sender, string connectionStatus)
    {
        TelemetryStatus = connectionStatus;
    }

    private void SharedMemoryBridgeLauncherOnConnectionStatusChanged(object? sender, BridgeLauncherEventArgs e)
    {
        BridgeConnectionStatus = e.ConnectionStatus;
    }

    partial void OnSelectedItemChanged(ManuItem value)
    {
        _navigationService.NavigateTo(value.ViewModelType);
        OnPropertyChanged(nameof(CurrentViewModel));
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (SelectedGame.RequiresSharedMemoryBridge)
        {
            await _sharedMemoryBridgeLauncher.LaunchBridgeAsync(SelectedGame);
        }
        
        _sharedFileReader.StartReading();
    }
}
