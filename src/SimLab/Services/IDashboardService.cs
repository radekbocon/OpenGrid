using System.Collections.Generic;
using SimLab.Models;

namespace SimLab.Services;

public interface IDashboardService
{
    bool IsRunning { get; }
    int Port { get; }
    void OpenInBrowser(DashboardInfo dashboard);
    void OpenInWebView(DashboardInfo dashboard);
    string GetDashboardUrl(DashboardInfo dashboard, bool useNetwork);
    void Stop();
}
