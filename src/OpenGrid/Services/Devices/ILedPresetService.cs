using OpenGrid.Models;

namespace OpenGrid.Services.Devices;

public interface ILedPresetService
{
    IReadOnlyList<LedPreset> GetAll();
    LedPreset? GetById(string? id);
    LedPreset Create(string name, LedPresetConfiguration? configuration = null);
    void Save(LedPreset preset);
    void Remove(string id);

    event EventHandler<LedPreset>? PresetAdded;
    event EventHandler<LedPreset>? PresetUpdated;
    event EventHandler<LedPreset>? PresetRemoved;
}
