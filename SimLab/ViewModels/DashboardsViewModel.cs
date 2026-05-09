using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using SimLab.Models;
using SimLab.Services;
using SimLab.Views;

namespace SimLab.ViewModels;

public partial class DashboardsViewModel : ViewModelBase
{
    private readonly ITelemetryService _telemetryService;

    public IReadOnlyList<DashboardInfo> Dashboards { get; }

    public DashboardsViewModel(ITelemetryService telemetryService)
    {
        _telemetryService = telemetryService;
        IsMenuItem = true;
        Dashboards =
        [
            new DashboardInfo
            {
                Name = "Simple",
                Description = "Speed, gear, inputs, RPM and tire temperatures",
                Style = DashboardStyle.Default,
            },
        ];
    }

    [RelayCommand]
    private void OpenDashboard(DashboardInfo dashboard)
    {
        var vm = new DashboardViewModel(_telemetryService);
        var window = new DashboardWindow()
        {
            DataContext = vm
        };
        window.Initialize(dashboard.Style);
        window.Show();
    }
}
