using System;
using OpenGrid.Models;

namespace OpenGrid.Services;

public interface IDashboardService
{
    bool IsRunning { get; }
    int Port { get; }
    DashboardInfo? ActiveDashboard { get; }
    event EventHandler<bool>? IsRunningChanged;
    void Start(DashboardInfo dashboard);
    string GetUrl(bool useNetwork);
    void OpenInBrowser();
    void Stop();
}
