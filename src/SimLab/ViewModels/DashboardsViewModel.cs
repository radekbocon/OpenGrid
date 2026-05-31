using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using SimLab.Models;
using SimLab.Services;
using SimLab.Views;

namespace SimLab.ViewModels;

public partial class DashboardsViewModel : ViewModelBase
{
    private readonly IDashboardRepository _repository;
    private readonly IDashboardService _dashboardService;

    public ObservableCollection<DashboardInfo> Dashboards { get; private set; } = [];
    
    public string StatusText => _dashboardService.IsRunning ? $"Server running on port {_dashboardService.Port}" : "Server not running";
    public bool IsServerRunning => _dashboardService.IsRunning;

    public DashboardsViewModel(IDashboardRepository repository, IDashboardService dashboardService)
    {
        _repository = repository;
        _dashboardService = dashboardService;
        IsMenuItem = true;
        ReloadDashboards();
        _dashboardService.IsRunningChanged += OnIsRunningChanged;
    }

    private void ReloadDashboards()
    {
        var dashboards = _repository.GetAllDashboards();
        Dashboards = new ObservableCollection<DashboardInfo>(dashboards);
    }

    private void OnIsRunningChanged(object? sender, bool isRunning)
    {
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(IsServerRunning));
    }

    [RelayCommand]
    private void OpenOnDevice(DashboardInfo dashboard)
    {
        _dashboardService.Start(dashboard);
        var url = _dashboardService.GetUrl(useNetwork: true);
        var dialog = new DeviceAccessDialog(dashboard.Name, url);
        DialogHost.Show(dialog);
    }

    [RelayCommand]
    private void OpenInBrowser(DashboardInfo dashboard)
    {
        _dashboardService.Start(dashboard);
        _dashboardService.OpenInBrowser();
    }
    
    [RelayCommand]
    private void StartServer()
    {
        _dashboardService.Start(Dashboards.First());
    }

    [RelayCommand]
    private void StopServer()
    {
        _dashboardService.Stop();
    }
}
