using SimLab.Models;

namespace SimLab.Services;

public interface IDashboardService
{
    bool IsRunning { get; }
    int Port { get; }
    DashboardInfo? ActiveDashboard { get; }
    void Start(DashboardInfo dashboard);
    string GetUrl(bool useNetwork);
    void OpenInBrowser();
    void OpenInWebView();
    void Stop();
}
