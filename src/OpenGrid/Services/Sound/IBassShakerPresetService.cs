using OpenGrid.Models;

namespace OpenGrid.Services.Sound;

public interface IBassShakerPresetService
{
    IReadOnlyList<BassShakerPreset> GetAll();
    BassShakerPreset? GetById(string? id);
    BassShakerPreset Create(string name, BassShakerConfiguration? configuration = null);
    void Save(BassShakerPreset preset);
    void Remove(string id);

    event EventHandler<BassShakerPreset>? PresetAdded;
    event EventHandler<BassShakerPreset>? PresetUpdated;
    event EventHandler<BassShakerPreset>? PresetRemoved;
}
