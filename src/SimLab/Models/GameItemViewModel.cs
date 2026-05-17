using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimLab.Services;
using SimLab.ViewModels;

namespace SimLab.Models;

public partial class GameItemViewModel : ViewModelBase
{
    private readonly SteamGameManager _gameManager;
    private readonly ITelemetryService _telemetryService;
    private SteamGameProcess? _game;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(ShowConnectButton))]
    [NotifyPropertyChangedFor(nameof(ShowLaunchAndConnectButton))]
    [NotifyPropertyChangedFor(nameof(ShowDisconnectButton))]
    [NotifyPropertyChangedFor(nameof(ShowCancelButton))]
    public partial bool IsRunning { get; set; }
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(ShowConnectButton))]
    [NotifyPropertyChangedFor(nameof(ShowLaunchAndConnectButton))]
    [NotifyPropertyChangedFor(nameof(ShowDisconnectButton))]
    [NotifyPropertyChangedFor(nameof(ShowCancelButton))]
    public partial TelemetryConnectionStatus TelemetryStatus { get; set; }
    public string? ImagePath => _game?.SteamGame.ImagePath;
    public string? Name => _game?.SteamGame.Name;
    public string StatusText => (IsRunning, TelemetryStatus) switch
    {
        (true, TelemetryConnectionStatus.Connected) => "Connected",
        (true, TelemetryConnectionStatus.Connecting) => "Connecting...",
        (true, TelemetryConnectionStatus.Disconnected) => "Running, Disconnected",
        (false, _) => "Not running",
        _ => ""
    };
    public bool ShowConnectButton => IsRunning && TelemetryStatus == TelemetryConnectionStatus.Disconnected;
    public bool ShowLaunchAndConnectButton => !IsRunning && TelemetryStatus == TelemetryConnectionStatus.Disconnected;
    public bool ShowDisconnectButton => TelemetryStatus == TelemetryConnectionStatus.Connected;
    public bool ShowCancelButton => TelemetryStatus == TelemetryConnectionStatus.Connecting;
    
    public GameItemViewModel(SteamGameManager gameManager, ITelemetryService telemetryService)
    {
        _gameManager = gameManager;
        _telemetryService = telemetryService;
        _telemetryService.TelemetryStatusChanged += OnTelemetryStatusChanged;
    }

    public bool Matches(SteamGameProcess game) => _game?.SteamGame.AppId == game.SteamGame.AppId;

    public void SetGame(SteamGameProcess game)
    {
        _game = game;
        IsRunning = _gameManager.IsRunning(game);
        
        if (_telemetryService.CurrentGame?.AppId == game.SteamGame.AppId)
        {
            TelemetryStatus = _telemetryService.ConnectionStatus;
        }
    }

    private void OnTelemetryStatusChanged(object? sender, TelemetryConnectionStatus status)
    {
        if (_telemetryService.CurrentGame?.AppId != _game?.SteamGame.AppId)
        {
            return;
        }

        TelemetryStatus = status;
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (_game is null)
        {
            return;
        }
        
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        
        await _telemetryService.ConnectAsync(_game.SteamGame, _cts.Token);
        _telemetryService.StartReading();
    }

    [RelayCommand]
    private async Task LaunchAndConnectAsync()
    {
        if (_game is null)
        {
            return;
        }
        
        _gameManager.LaunchGame(_game);
        while (!_gameManager.IsRunning(_game))
        {
            await Task.Delay(1000);
        }

        await ConnectAsync();
    }

    [RelayCommand]
    private void Disconnect()
    {
        _telemetryService.StopReading();
    }
    
    [RelayCommand]
    private void Cancel()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
