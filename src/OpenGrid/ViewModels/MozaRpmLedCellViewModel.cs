using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenGrid.Models;

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

    public MozaRpmLedCellViewModel(int index, string? value, Action onChanged)
    {
        Index = index;
        _onChanged = onChanged;

        var normalized = string.IsNullOrWhiteSpace(value) ? RpmLedSegment.OffLed : value.Trim();
        var rgb = RpmLedSegment.TryParseLed(normalized);

        _value = normalized;
        _color = rgb is { } parsed
            ? Color.FromRgb(parsed.R, parsed.G, parsed.B)
            : Colors.Black;
    }

    public int Index { get; }

    [ObservableProperty]
    private string _value;

    [ObservableProperty]
    private Color _color;

    partial void OnValueChanged(string value)
    {
        if (_updating)
        {
            return;
        }

        _updating = true;
        Color = RpmLedSegment.TryParseLed(value) is { } rgb
            ? Color.FromRgb(rgb.R, rgb.G, rgb.B)
            : Colors.Black;
        _updating = false;
        _onChanged();
    }

    partial void OnColorChanged(Color value)
    {
        if (_updating)
        {
            return;
        }

        _updating = true;
        Value = ToHex(value);
        _updating = false;
        _onChanged();
    }

    private static string ToHex(Color color) =>
        $"#{color.R:X2}{color.G:X2}{color.B:X2}";
}
