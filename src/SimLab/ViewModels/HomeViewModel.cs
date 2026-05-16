using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using SimLab.Models;
using SimLab.Services;

namespace SimLab.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly ITelemetryService _telemetryService;
    private readonly SteamGameManager _gameManager;

    public ObservableCollection<GameItemViewModel> Games { get; } = [];

    public HomeViewModel(ITelemetryService telemetryService, SteamGameManager gameManager)
    {
        _telemetryService = telemetryService;
        _gameManager = gameManager;
        IsMenuItem = true;
    }

    [RelayCommand]
    private void Loaded()
    {
        _gameManager.GameStarted += OnGameStarted;
        _gameManager.GameStopped += OnGameStopped;
        DetectGames();
    }

    [RelayCommand]
    private void Unloaded()
    {
        _gameManager.GameStarted -= OnGameStarted;
        _gameManager.GameStopped -= OnGameStopped;
    }

    private void OnGameStarted(SteamGameProcess game)
    {
        var vm = FindGameVm(game);
        if (vm != null)
            vm.IsRunning = true;
    }

    private void OnGameStopped(SteamGameProcess game)
    {
        var vm = FindGameVm(game);
        if (vm != null)
            vm.IsRunning = false;
    }

    private GameItemViewModel? FindGameVm(SteamGameProcess game)
    {
        foreach (var vm in Games)
        {
            if (vm.Matches(game))
                return vm;
        }
        return null;
    }

    private void DetectGames()
    {
        var games = _gameManager.GetInstalledGames();
        Games.Clear();
        foreach (var steamGameProcess in games)
        {
            var vm = new GameItemViewModel();
            vm.SetGame(steamGameProcess, _gameManager, _telemetryService);
            vm.IsRunning = _gameManager.IsRunning(steamGameProcess);
            Games.Add(vm);
        }
    }
}
