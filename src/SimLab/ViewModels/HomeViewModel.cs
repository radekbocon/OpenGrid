using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using SimLab.Models;
using SimLab.Services;

namespace SimLab.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly ITelemetryService _telemetryService;
    private readonly IGameService _gameService;

    public ObservableCollection<GameItem> Games { get; } = [];

    public HomeViewModel(ITelemetryService telemetryService, IGameService gameService)
    {
        _telemetryService = telemetryService;
        _gameService = gameService;
        IsMenuItem = true;
    }

    [RelayCommand]
    private void Loaded()
    {
        DetectGames();
    }

    [RelayCommand]
    private void Unloaded()
    {
    }


    private void DetectGames()
    {
        Games.Clear();
        var detectedGames = _gameService.DetectGames();
        foreach (var game in detectedGames)
        {
            Games.Add(game);
        }
    }

    [RelayCommand]
    private async Task ConnectAsync(GameItem gameItem)
    {
        await _gameService.ConnectAsync(gameItem);
    }

    [RelayCommand]
    private async Task LaunchAndConnectAsync(GameItem gameItem)
    {
        await _gameService.LaunchAndConnectAsync(gameItem);
    }

    [RelayCommand]
    private void Disconnect()
    {
        _telemetryService.StopReading();
    }
}