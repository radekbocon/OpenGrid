using System.Collections.ObjectModel;
using OpenGrid.Models;

namespace OpenGrid.Services;

public interface IDashboardService
{
    ObservableCollection<DashboardInfo> Dashboards { get; }
    IEnumerable<DashboardInfo> GetByType(DashboardType type);
    DashboardInfo? GetById(string id);
    void Scan();
}
