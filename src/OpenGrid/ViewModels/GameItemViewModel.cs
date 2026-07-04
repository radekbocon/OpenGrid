using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenGrid.Models;
using OpenGrid.Services;
using OpenGrid.Services.Telemetry;

namespace OpenGrid.ViewModels;

public partial class GameItemViewModel : ViewModelBase
{
    private readonly SteamGameManager _gameManager;
    private readonly ITelemetryService _telemetryService;
    private readonly ISettingsService _settingsService;
    private SteamGameProcess? _game;

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
        (true, TelemetryConnectionStatus.Error) => GetErrorMessage(),
        (false, _) => "Not running",
        _ => ""
    };

    private string GetErrorMessage()
    {
        return TelemetrySetupHelper.AdditionalSetupNeeded(_game?.SteamGame)
            ? "Telemetry setup required. Click info button for details."
            : "Error connecting";
    }

    public bool ShowConnectButton => IsRunning && TelemetryStatus is TelemetryConnectionStatus.Disconnected or TelemetryConnectionStatus.Error;
    public bool ShowLaunchAndConnectButton => !IsRunning && TelemetryStatus is TelemetryConnectionStatus.Disconnected or TelemetryConnectionStatus.Error;
    public bool ShowDisconnectButton => TelemetryStatus == TelemetryConnectionStatus.Connected;
    public bool ShowCancelButton => TelemetryStatus == TelemetryConnectionStatus.Connecting;

    public bool ShowSetupButton => TelemetrySetupHelper.AdditionalSetupNeeded(_game?.SteamGame);

    [ObservableProperty]
    public partial bool IsAutoConnectEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsAutoRecordingEnabled { get; set; }

    public GameItemViewModel(SteamGameManager gameManager, ITelemetryService telemetryService, ISettingsService settingsService)
    {
        _gameManager = gameManager;
        _telemetryService = telemetryService;
        _settingsService = settingsService;
        _telemetryService.TelemetryStatusChanged += OnTelemetryStatusChanged;
    }

    public bool Matches(SteamGameProcess game) => _game?.SteamGame.AppId == game.SteamGame.AppId;

    public void SetGame(SteamGameProcess game)
    {
        _game = game;
        IsRunning = _gameManager.IsRunning(game);
        IsAutoConnectEnabled = _settingsService.AutoConnectGameAppIds.Contains(_game.SteamGame.AppId);
        IsAutoRecordingEnabled = _settingsService.AutoRecordingGameAppIds.Contains(_game.SteamGame.AppId);
        
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
        
        await _telemetryService.ConnectAsync(_game.SteamGame);
    }

    [RelayCommand]
    private async Task LaunchAndConnectAsync()
    {
        if (_game is null)
        {
            return;
        }
        
        var launchCts = new CancellationTokenSource();
        launchCts.CancelAfter(30000);
        
        _gameManager.LaunchGame(_game);
        while (!_gameManager.IsRunning(_game))
        {
            if (launchCts.IsCancellationRequested)
            {
                return;
            }
            await Task.Delay(1000);
        }

        await ConnectAsync();
    }

    [RelayCommand]
    private void Disconnect()
    {
        _telemetryService.Disconnect();
    }
    
    [RelayCommand]
    private void Cancel()
    {
        _telemetryService.Disconnect();
    }

    [RelayCommand]
    private async Task ShowTelemetrySetupAsync()
    {
        await TelemetrySetupHelper.HandleTelemetrySetupAsync(_game?.SteamGame);
    }

    partial void OnIsAutoConnectEnabledChanged(bool value)
    {
        if (_game?.SteamGame.AppId is not {} appId)
        {
            return;
        }
        
        if (value)
        {
            _settingsService.AutoConnectGameAppIds.Add(appId);
        }
        else
        {
            _settingsService.AutoConnectGameAppIds.Remove(appId);
            IsAutoRecordingEnabled = false;
        }
    }

    partial void OnIsAutoRecordingEnabledChanged(bool value)
    {
        if (_game?.SteamGame.AppId is not {} appId)
        {
            return;
        }
        
        if (value)
        {
            _settingsService.AutoRecordingGameAppIds.Add(appId);
            IsAutoConnectEnabled = true;
        }
        else
        {
            _settingsService.AutoRecordingGameAppIds.Remove(appId);
        }
    }
}
