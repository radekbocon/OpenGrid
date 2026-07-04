using System.Collections.Generic;

namespace OpenGrid.Services;

public interface ISettingsService
{
    bool MinimizeToTray { get; set; }
    string? SelectedTheme { get; set; }
    int RecordingRateHz { get; set; }
    HashSet<int> AutoConnectGameAppIds { get; set; }
    HashSet<int> AutoRecordingGameAppIds { get; set; }
    void Save();
    void Load();
}
