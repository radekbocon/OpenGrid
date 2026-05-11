using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimLab.Models;
using SimLab.Services;
using SimLab.Views;

namespace SimLab.ViewModels;

public partial class DashboardsViewModel : ViewModelBase
{
    private readonly IDashboardRepository _repository;
    private readonly IDashboardService _dashboardService;
    private CancellationTokenSource? _pollCts;

    public ObservableCollection<DashboardInfo> Dashboards { get; } = [];

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
        Dashboards.Clear();
        foreach (var d in _repository.GetAllDashboards())
        {
            Dashboards.Add(d);
        }
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
                        StatusText = null;
                        HasDevices = false;
                    }
                });
            }
        }, ct);
    }

    [RelayCommand]
    private void OpenOnThisMachine(DashboardInfo dashboard)
    {
        _dashboardService.OpenInBrowser(dashboard);
    }

    [RelayCommand]
    private void OpenOnDevice(DashboardInfo dashboard)
    {
        var url = _dashboardService.GetDashboardUrl(dashboard, useNetwork: true);
        var dialog = new DeviceAccessDialog(dashboard.Name, url);
        dialog.Show();
    }

    [RelayCommand]
    private void OpenInBrowser(DashboardInfo dashboard)
    {
        _dashboardService.OpenInBrowser(dashboard);
    }
    
    [RelayCommand]
    private void StopServer()
    {
        _dashboardService.Stop();
    }
}
