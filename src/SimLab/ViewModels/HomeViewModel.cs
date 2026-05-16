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
        DetectGames();
    }

    [RelayCommand]
    private void Unloaded()
    {
    }


    private void DetectGames()
    {
        var games = _gameManager.GetInstalledGames();
        Games.Clear();
        foreach (var steamGameProcess in games)
        {
            var vm = new GameItemViewModel();
            vm.SetGame(steamGameProcess);
            Games.Add(vm);
        }
    }
}