using System.Collections.Generic;
using SimLab.Models;

namespace SimLab.Services;

public interface IDashboardRepository
{
    IReadOnlyList<DashboardInfo> GetAllDashboards();
    IReadOnlyList<DashboardInfo> GetSystemDashboards();
    IReadOnlyList<DashboardInfo> GetUserDashboards();
    DashboardInfo? GetById(string id);
    string GetDashboardDirectory(DashboardInfo dashboard);
}
