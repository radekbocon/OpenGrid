namespace SimLab.Services;

public interface ISettingsService
{
    bool MinimizeToTray { get; set; }
    void Save();
    void Load();
}
