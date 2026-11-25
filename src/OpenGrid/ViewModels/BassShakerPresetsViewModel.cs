using OpenGrid.Models;
using OpenGrid.Services.Sound;

namespace OpenGrid.ViewModels;

public partial class BassShakerPresetsViewModel : PresetListViewModel<BassShakerPreset>
{
    private readonly IBassShakerPresetService _presetService;

    public BassShakerPresetsViewModel(IBassShakerPresetService presetService)
    {
        _presetService = presetService;
        InitializePresets();
    }

    protected override IReadOnlyList<BassShakerPreset> GetPresets() => _presetService.GetAll();

    protected override BassShakerPreset CreatePreset(string name, BassShakerPreset? copyFrom)
    {
        return _presetService.Create(name, copyFrom?.Configuration);
    }

    protected override void SavePreset(BassShakerPreset preset) => _presetService.Save(preset);

    protected override void RemovePreset(BassShakerPreset preset) => _presetService.Remove(preset.Id);

    protected override string GetPresetName(BassShakerPreset preset) => preset.Name;

    protected override void SetPresetName(BassShakerPreset preset, string name) => preset.Name = name;

    protected override string GetPresetSummary(BassShakerPreset preset)
    {
        return preset.Configuration.Inputs.Count == 1
            ? "1 effect"
            : $"{preset.Configuration.Inputs.Count} effects";
    }

    protected override object? CreateEditor(BassShakerPreset preset)
    {
        return new BassShakerConfigurationViewModel(preset.Configuration, OnEditorChanged);
    }

    protected override PresetItemViewModel<BassShakerPreset> CreateItem(BassShakerPreset preset)
    {
        return new BassShakerPresetItemViewModel(preset, _presetService);
    }
}
