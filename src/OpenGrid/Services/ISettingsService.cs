namespace OpenGrid.Services;

public interface ISettingsService
{
    bool MinimizeToTray { get; set; }
    string? SelectedTheme { get; set; }
    int RecordingRateHz { get; set; }
    bool DriveMozaWheelLedsFromTelemetry { get; set; }
    ObservableSet<int> AutoConnectGameAppIds { get; }
    ObservableSet<int> AutoRecordingGameAppIds { get; }
    event EventHandler? SettingsChanged;
    void Save();
    void Load();
}
