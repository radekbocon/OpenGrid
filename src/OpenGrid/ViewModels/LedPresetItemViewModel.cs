using OpenGrid.Models;
using OpenGrid.Services.Devices;

namespace OpenGrid.ViewModels;

public sealed class LedPresetItemViewModel : PresetItemViewModel<LedPreset>
{
    public LedPresetItemViewModel(LedPreset preset, ILedPresetService presetService)
        : base(
            preset,
            presetService.Save,
            static x => x.Name,
            static (x, name) => x.Name = name,
            static x =>
            {
                var frames = x.Configuration.RpmSegments.Count;
                return $"{frames} {(frames == 1 ? "frame" : "frames")} · {x.Configuration.LedCount} LEDs";
            })
    {
    }
}
