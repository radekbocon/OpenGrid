using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimLab.Services;
using SimLab.ViewModels;

namespace SimLab.Models;

public partial class GameItemViewModel : ViewModelBase
{
    private SteamGameProcess _game = null!;
    private SteamGameManager _gameManager = null!;
    private ITelemetryService _telemetryService = null!;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(ShowConnectButton))]
    [NotifyPropertyChangedFor(nameof(ShowLaunchAndConnectButton))]
    private bool _isRunning;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(ShowConnectButton))]
    [NotifyPropertyChangedFor(nameof(ShowLaunchAndConnectButton))]
    [NotifyPropertyChangedFor(nameof(ShowDisconnectButton))]
    private bool _isConnected;

    public string ImagePath => _game.SteamGame.ImagePath;
    public string Name => _game.SteamGame.Name;

    public string StatusText => IsConnected ? "Connected" : IsRunning ? "Running" : "Ready";
    public bool ShowConnectButton => IsRunning && !IsConnected;
    public bool ShowLaunchAndConnectButton => !IsRunning && !IsConnected;
    public bool ShowDisconnectButton => IsConnected;

    public bool Matches(SteamGameProcess game) => _game.SteamGame.AppId == game.SteamGame.AppId;

    public void SetGame(SteamGameProcess game, SteamGameManager gameManager, ITelemetryService telemetryService)
    {
        _game = game;
        _gameManager = gameManager;
        _telemetryService = telemetryService;
        _telemetryService.TelemetryStatusChanged += OnTelemetryStatusChanged;

        IsRunning = _gameManager.IsRunning(game);
    }

    private void OnTelemetryStatusChanged(object? sender, TelemetryConnectionStatus status)
    {
        if (_telemetryService.CurrentGame?.AppId != _game.SteamGame.AppId)
            return;

        IsConnected = status == TelemetryConnectionStatus.Connected;
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        await _telemetryService.ConnectAsync(_game.SteamGame, CancellationToken.None);
        _telemetryService.StartReading();
    }

    [RelayCommand]
    private async Task LaunchAndConnectAsync()
    {
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
}
