namespace SimLab.Services;

public interface ISettingsService
{
    bool MinimizeToTray { get; set; }
    int? DashboardPort { get; set; }
    string? SelectedTheme { get; set; }
    int RecordingRateHz { get; set; }
    void Save();
    void Load();
}
