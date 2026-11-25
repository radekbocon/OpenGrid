using OpenGrid.Models;
using OpenGrid.Services.Sound;

namespace OpenGrid.ViewModels;

public sealed class BassShakerPresetItemViewModel : PresetItemViewModel<BassShakerPreset>
{
    public BassShakerPresetItemViewModel(BassShakerPreset preset, IBassShakerPresetService presetService)
        : base(
            preset,
            presetService.Save,
            static x => x.Name,
            static (x, name) => x.Name = name,
            static x => x.Configuration.Inputs.Count == 1
                ? "1 effect"
                : $"{x.Configuration.Inputs.Count} effects")
    {
    }
}
