using Avalonia.Controls;
using Avalonia.Interactivity;
using DialogHostAvalonia;
using OpenGrid.Models;

namespace OpenGrid.Controls;

public partial class AddGameDashboardOverrideDialog : UserControl
{
    public IReadOnlyList<SteamGame> Games { get; }
    public IReadOnlyList<DashboardInfo> Dashboards { get; }
    public SteamGame? SelectedGame { get; set; }
    public DashboardInfo? SelectedDashboard { get; set; }
    public GameDashboardOverride? Result { get; private set; }

    public AddGameDashboardOverrideDialog(IEnumerable<SteamGame> games, IEnumerable<DashboardInfo> dashboards, IEnumerable<int> usedAppIds)
    {
        InitializeComponent();
        Games = games.Where(game => !usedAppIds.Contains(game.AppId)).ToList();
        Dashboards = dashboards.ToList();
        SelectedGame = Games.FirstOrDefault();
        SelectedDashboard = Dashboards.FirstOrDefault();
        DataContext = this;
        AddButton.IsEnabled = SelectedGame is not null && SelectedDashboard is not null;
    }

    private void AddClick(object? sender, RoutedEventArgs e)
    {
        if (SelectedGame is null || SelectedDashboard is null)
            return;

        Result = new GameDashboardOverride
        {
            GameAppId = SelectedGame.AppId,
            DashboardId = SelectedDashboard.Id
        };
        DialogHost.Close(null);
    }

    private void CancelClick(object? sender, RoutedEventArgs e)
    {
        Result = null;
        DialogHost.Close(null);
    }
}
