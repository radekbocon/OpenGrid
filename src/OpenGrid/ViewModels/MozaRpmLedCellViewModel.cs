using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenGrid.ViewModels;

/// <summary>
/// One LED in a frame. The UI edits <see cref="Color"/> with an Avalonia
/// ColorPicker; black means the LED is off. <see cref="Value"/> mirrors the
/// colour for persistence and the MOZA wire protocol.
/// </summary>
public partial class MozaRpmLedCellViewModel : ObservableObject
{
    private readonly Action _onChanged;
    private bool _updating;

    public MozaRpmLedCellViewModel(int index, Color color, Action onChanged)
    {
        Index = index;
        _onChanged = onChanged;
        Color = color;
    }

    public int Index { get; }

    [ObservableProperty]
    public partial Color Color { get; set; }

    partial void OnColorChanged(Color value)
    {
        if (_updating)
        {
            return;
        }

        _updating = true;
        Color = value;
        _updating = false;
        _onChanged();
    }
}
