using OpenGrid.Models;
using OpenGrid.Services.Devices;

namespace OpenGrid.ViewModels;

public sealed class LedPresetItemViewModel : PresetItemViewModel<LedPreset>
{
    public LedPresetItemViewModel(
        LedPreset preset,
        ILedPresetService presetService,
        Action<PresetItemViewModel<LedPreset>> duplicate,
        Func<PresetItemViewModel<LedPreset>, Task> remove)
        : base(
            preset,
            presetService.Save,
            static x => x.Name,
            static (x, name) => x.Name = name,
            static x =>
            {
                var effects = x.Configuration.Effects.Count;
                return $"{effects} {(effects == 1 ? "effect" : "effects")} · {x.Configuration.LedCount} LEDs";
            },
            duplicate,
            remove)
    {
    }
}
