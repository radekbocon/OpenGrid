namespace OpenGrid.Services;

public interface ISettingsService
{
    bool MinimizeToTray { get; set; }
    string? SelectedTheme { get; set; }
    int RecordingRateHz { get; set; }
    ObservableSet<int> AutoConnectGameAppIds { get; }
    ObservableSet<int> AutoRecordingGameAppIds { get; }
    void Save();
    void Load();
}
