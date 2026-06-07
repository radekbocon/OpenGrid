using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using SimLab.Controls;
using SimLab.Models;
using SimLab.Services;
using SimLab.Services.Telemetry;

namespace SimLab.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly ITelemetryService _telemetryService;
    private readonly SteamGameManager _steamGameManager;

    public ObservableCollection<GameItemViewModel> Games { get; } = [];

    public HomeViewModel(ITelemetryService telemetryService, SteamGameManager steamGameManager)
    {
        _telemetryService = telemetryService;
        _steamGameManager = steamGameManager;
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

    private void OnSteamGameStarted(SteamGameProcess game)
    {
        var vm = FindGameViewModel(game);
        vm?.IsRunning = true;
    }

    private void OnSteamGameStopped(SteamGameProcess game)
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
            var vm = new GameItemViewModel(_steamGameManager, _telemetryService);
            vm.SetGame(steamGameProcess);
            Games.Add(vm);
        }
#if DEBUG
        var debugVm = new GameItemViewModel(_steamGameManager, _telemetryService);
        debugVm.SetGame(new SteamGameProcess
        {
            SteamGame = SteamGame.Debug
        });
        Games.Add(debugVm);
#endif
    }
}