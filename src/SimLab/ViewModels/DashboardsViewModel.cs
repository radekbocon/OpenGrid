using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
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
    private CancellationTokenSource? _pollCts;

    public ObservableCollection<DashboardInfo> Dashboards { get; private set; } = [];

    [ObservableProperty]
    public partial string? StatusText { get; set; }

    [ObservableProperty]
    public partial bool IsServerRunning { get; set; }

    [ObservableProperty]
    public partial bool HasDevices { get; set; }

    public DashboardsViewModel(IDashboardRepository repository, IDashboardService dashboardService)
    {
        _repository = repository;
        _dashboardService = dashboardService;
        IsMenuItem = true;
        ReloadDashboards();
        StartPolling();
    }

    private void ReloadDashboards()
    {
        var dashboards = _repository.GetAllDashboards();
        Dashboards = new ObservableCollection<DashboardInfo>(dashboards);
    }

    private void StartPolling()
    {
        _pollCts?.Cancel();
        _pollCts = new CancellationTokenSource();
        var ct = _pollCts.Token;

        Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(2000, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                Dispatcher.UIThread.Post(() =>
                {
                    IsServerRunning = _dashboardService.IsRunning;
                    if (_dashboardService.IsRunning)
                    {
                        StatusText = $"Server running on port {_dashboardService.Port}";
                    }
                    else
                    {
                        StatusText = "Server not running";
                        HasDevices = false;
                    }
                });
            }
        }, ct);
    }

    [RelayCommand]
    private void OpenOnThisMachine(DashboardInfo dashboard)
    {
        _dashboardService.Start(dashboard);
        _dashboardService.OpenInBrowser();
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
