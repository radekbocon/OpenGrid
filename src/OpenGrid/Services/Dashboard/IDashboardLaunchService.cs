using OpenGrid.Models;

namespace OpenGrid.Services.Dashboard;

public interface IDashboardLaunchService
{
    void HandleTrigger(DashboardLaunchTrigger trigger);
    void CloseAll();
}
