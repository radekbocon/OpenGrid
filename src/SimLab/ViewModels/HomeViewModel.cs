using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
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
        _gameService.GameProcessChanged += GameServiceOnGameProcessChanged;
        DetectGames();
    }
    
    [RelayCommand]
    private void Unloaded()
    {
        _gameService.GameProcessChanged -= GameServiceOnGameProcessChanged;
    }

    private void GameServiceOnGameProcessChanged(object? sender, GameProcessEventArgs e)
    {
        DetectGames();
    }


    private void DetectGames()
    {
        var installed = _gameService.GetInstalledGameIds();
        var running = _gameService.GetRunningGameProcesses();

        Games.Clear();

        foreach (var game in SteamGame.GetAll().Where(g => g != SteamGame.Debug))
        {
            var isInstalled = installed.Contains(game.AppId);
            var isRunning = running.Contains(game.AppId);
            Games.Add(new GameItem(game, isInstalled, isRunning));
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
