namespace SimLab.Services;

public interface ISettingsService
{
    bool MinimizeToTray { get; set; }
    int? DashboardPort { get; set; }
    void Save();
    void Load();
}
