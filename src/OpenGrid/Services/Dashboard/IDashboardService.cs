using System.Collections.ObjectModel;
using OpenGrid.Models;

namespace OpenGrid.Services.Dashboard;

public interface IDashboardService
{
    ObservableCollection<DashboardInfo> Dashboards { get; }
    DashboardInfo? GetById(string id);
    void Scan();
}
