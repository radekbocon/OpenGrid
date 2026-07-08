using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using OpenGrid.Controls;
using OpenGrid.Models;
using OpenGrid.Services;
using OpenGrid.Services.Telemetry;

namespace OpenGrid.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly ITelemetryService _telemetryService;
    private readonly SteamGameManager _steamGameManager;
    private readonly ISettingsService _settingsService;

    public ObservableCollection<GameItemViewModel> Games { get; } = [];

    public HomeViewModel(ITelemetryService telemetryService, SteamGameManager steamGameManager, ISettingsService settingsService)
    {
        _telemetryService = telemetryService;
        _steamGameManager = steamGameManager;
        _settingsService = settingsService;
        IsMenuItem = true;
    }

    protected override Task OnLoadedAsync()
    {
        _steamGameManager.GameStarted += OnSteamGameStarted;
        _steamGameManager.GameStopped += OnSteamGameStopped;
        SetInstalledGames();
        
        return base.OnLoadedAsync();
    }

    protected override Task OnUnloadedAsync()
    {
        _steamGameManager.GameStarted -= OnSteamGameStarted;
        _steamGameManager.GameStopped -= OnSteamGameStopped;
        
        return base.OnUnloadedAsync();
    }

    [RelayCommand]
    private async Task ShowAllSupportedGamesAsync()
    {
        var dialog = new AllSupportedGamesDialog();
        await DialogHost.Show(dialog);
    }

    private void OnSteamGameStarted(object? sender, SteamGameProcess game)
    {
        var vm = FindGameViewModel(game);
        vm?.IsRunning = true;
    }

    private void OnSteamGameStopped(object? sender,SteamGameProcess game)
    {
        var vm = FindGameViewModel(game);
        vm?.IsRunning = false;
    }

    private GameItemViewModel? FindGameViewModel(SteamGameProcess game)
    {
        return Games.FirstOrDefault(vm => vm.Matches(game));
    }

    private void SetInstalledGames()
    {
        var games = _steamGameManager.GetInstalledGames();
        Games.Clear();
        foreach (var steamGameProcess in games)
        {
            var vm = new GameItemViewModel(_steamGameManager, _telemetryService, _settingsService);
            vm.SetGame(steamGameProcess);
            Games.Add(vm);
        }
#if DEBUG
        var debugVm = new GameItemViewModel(_steamGameManager, _telemetryService, _settingsService);
        debugVm.SetGame(new SteamGameProcess
        {
            SteamGame = SteamGame.Debug
        });
        Games.Add(debugVm);
#endif
    }

}