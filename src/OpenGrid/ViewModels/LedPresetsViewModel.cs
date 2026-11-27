using OpenGrid.Models;
using OpenGrid.Services.Devices;

namespace OpenGrid.ViewModels;

public partial class LedPresetsViewModel : PresetListViewModel<LedPreset>
{
    private readonly ILedPresetService _presetService;

    public LedPresetsViewModel(ILedPresetService presetService)
    {
        _presetService = presetService;
        InitializePresets();
    }

    protected override IReadOnlyList<LedPreset> GetPresets() => _presetService.GetAll();

    protected override LedPreset CreatePreset(string name, LedPreset? copyFrom)
    {
        return _presetService.Create(name, copyFrom?.Configuration);
    }

    protected override void SavePreset(LedPreset preset) => _presetService.Save(preset);

    protected override void RemovePreset(LedPreset preset) => _presetService.Remove(preset.Id);

    protected override string GetPresetName(LedPreset preset) => preset.Name;

    protected override void SetPresetName(LedPreset preset, string name) => preset.Name = name;

    protected override string GetPresetSummary(LedPreset preset)
    {
        var effects = preset.Configuration.Effects.Count;
        return $"{effects} {(effects == 1 ? "effect" : "effects")} · {preset.Configuration.LedCount} LEDs";
    }

    protected override object? CreateEditor(LedPreset preset)
    {
        return new LedConfigurationViewModel(preset.Configuration, OnEditorChanged);
    }

    protected override PresetItemViewModel<LedPreset> CreateItem(
        LedPreset preset,
        Action<PresetItemViewModel<LedPreset>> duplicate,
        Func<PresetItemViewModel<LedPreset>, Task> remove)
    {
        return new LedPresetItemViewModel(preset, _presetService, duplicate, remove);
    }
}
