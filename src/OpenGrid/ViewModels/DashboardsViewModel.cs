using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using OpenGrid.Services;
using OpenGrid.Services.Dashboard;

namespace OpenGrid.ViewModels;

public partial class DashboardsViewModel : ViewModelBase
{
    private readonly IDashboardService _dashboardService;
    private readonly DashboardHttpServer _dashboardServer;

    public ObservableCollection<DashboardCardViewModel> Dashboards { get; } = [];

    public DashboardsViewModel(IDashboardService dashboardService, DashboardHttpServer dashboardServer)
    {
        _dashboardService = dashboardService;
        _dashboardServer = dashboardServer;
        IsMenuItem = true;
    }

    protected override Task OnLoadedAsync()
    {
        Refresh();
        return base.OnLoadedAsync();
    }

    [RelayCommand]
    private void Refresh()
    {
        Dashboards.Clear();
        _dashboardService.Scan();
        foreach (var item in _dashboardService.Dashboards)
        {
            Dashboards.Add(new DashboardCardViewModel(item, _dashboardServer));
        }
    }
}
