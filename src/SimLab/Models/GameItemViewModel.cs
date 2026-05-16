using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using SimLab.Services;
using SimLab.ViewModels;

namespace SimLab.Models;

public partial class GameItemViewModel : ViewModelBase
{
    private SteamGameProcess _game;

    public string StatusText { get; }
    public string ImagePath => _game.SteamGame.ImagePath;
    public string Name => _game.SteamGame.Name;
    public bool ShowConnectButton { get; }
    public bool ShowLaunchAndConnectButton { get; }

    public void SetGame(SteamGameProcess game)
    {
        _game = game;
    }
    
    
    [RelayCommand]
    private async Task ConnectAsync()
    {
    }

    [RelayCommand]
    private async Task LaunchAndConnectAsync()
    {
    }

    [RelayCommand]
    private void Disconnect()
    {
    }
}
