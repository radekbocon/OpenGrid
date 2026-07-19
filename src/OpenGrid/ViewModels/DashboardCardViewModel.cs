using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using OpenGrid.Models;
using OpenGrid.Services.Dashboard;
using OpenGrid.Views;

namespace OpenGrid.ViewModels;

public partial class DashboardCardViewModel : ObservableObject
{
    private readonly DashboardHttpServer _dashboardServer;
    public DashboardInfo Dashboard { get; }

    public DashboardCardViewModel(DashboardInfo dashboard, DashboardHttpServer dashboardServer)
    {
        Dashboard = dashboard;
        _dashboardServer = dashboardServer;
    }

    private string DashboardUrlFor(DashboardInfo dashboard) =>
        $"http://localhost:{_dashboardServer.Port}/dashboards/{dashboard.Id}/";

    private string DashboardUrl => DashboardUrlFor(Dashboard);

    private string DeviceDashboardUrlFor(DashboardInfo dashboard) =>
        $"http://{GetLocalIpAddress()}:{_dashboardServer.Port}/dashboards/{dashboard.Id}/";

    [RelayCommand]
    private async Task OpenInBrowserAsync()
    {
        var url = DashboardUrl;
        if (string.IsNullOrEmpty(url))
            return;
        await Launcher.LaunchUriAsync(url);
    }

    [RelayCommand]
    private void OpenInApp()
    {
        var window = new DashboardWindow(DashboardUrl, Dashboard.Name, Dashboard.Width, Dashboard.Height);
        window.Show();
    }

    [RelayCommand]
    private async Task ShowOnDeviceAsync()
    {
        var url = DeviceDashboardUrlFor(Dashboard);
        var dialog = new DeviceAccessDialog(Dashboard.Name, url);
        await DialogHost.Show(dialog);
    }

    private static string GetLocalIpAddress()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    return ip.ToString();
            }
        }
        catch
        {
            // Ignore
        }

        return "127.0.0.1";
    }
}
