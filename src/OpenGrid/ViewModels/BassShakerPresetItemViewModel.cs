using CommunityToolkit.Mvvm.ComponentModel;
using OpenGrid.Models;
using OpenGrid.Services.Sound;

namespace OpenGrid.ViewModels;

public partial class BassShakerPresetItemViewModel : ViewModelBase
{
    private readonly IBassShakerPresetService _presetService;

    public BassShakerPreset Preset { get; }

    public string Id => Preset.Id;

    public string Name
    {
        get => Preset.Name;
        set
        {
            if (Preset.Name == value)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                // Revert the editor to the current name instead of allowing an empty preset name.
                OnPropertyChanged();
                return;
            }

            Preset.Name = value.Trim();
            _presetService.Save(Preset);
            OnPropertyChanged();
            RefreshSummary();
        }
    }

    public string Summary => Preset.Configuration.Inputs.Count == 1
        ? "1 effect"
        : $"{Preset.Configuration.Inputs.Count} effects";

    public BassShakerPresetItemViewModel(BassShakerPreset preset, IBassShakerPresetService presetService)
    {
        Preset = preset;
        _presetService = presetService;
    }

    public void RefreshSummary()
    {
        OnPropertyChanged(nameof(Summary));
    }
}
