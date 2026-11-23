using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenGrid.Models;

namespace OpenGrid.ViewModels;

/// <summary>
/// One complete RPM LED frame: an exact colour for every LED, the redline
/// percentage that activates it, and an optional flicker flag.
/// </summary>
public partial class MozaRpmSegmentViewModel : ObservableObject
{
    private readonly Action _onChanged;
    private readonly Action<MozaRpmSegmentViewModel> _onRemove;

    public MozaRpmSegmentViewModel(
        RpmLedSegment segment,
        int frameLength,
        Action onChanged,
        Action<MozaRpmSegmentViewModel> onRemove)
    {
        _onChanged = onChanged;
        _onRemove = onRemove;

        _thresholdPercentText = Math.Clamp(segment.ThresholdPercent, 1, 100).ToString();
        _flicker = segment.Flicker;

        var count = Math.Clamp(frameLength, 1, RpmLedSegment.MaxLeds);
        for (var i = 0; i < count; i++)
        {
            Leds.Add(new MozaRpmLedCellViewModel(
                i + 1,
                i < segment.Leds.Count ? segment.Leds[i] : RpmLedSegment.OffLed,
                _onChanged));
        }
    }

    [ObservableProperty]
    private int _index;

    [ObservableProperty]
    private string _thresholdPercentText;

    [ObservableProperty]
    private bool _flicker;

    public ObservableCollection<MozaRpmLedCellViewModel> Leds { get; } = [];

    public int ThresholdPercent => ParseInt(ThresholdPercentText, 1, 100);

    public RpmLedSegment ToModel() => new(
        Leds.Select(x => x.Color).ToList(),
        ThresholdPercent,
        Flicker);

    public void Resize(int frameLength)
    {
        var count = Math.Clamp(frameLength, 1, RpmLedSegment.MaxLeds);

        while (Leds.Count < count)
        {
            Leds.Add(new MozaRpmLedCellViewModel(Leds.Count + 1, RpmLedSegment.OffLed, _onChanged));
        }

        if (Leds.Count > count)
        {
            while (Leds.Count > count)
            {
                Leds.RemoveAt(Leds.Count - 1);
            }
        }
    }

    partial void OnThresholdPercentTextChanged(string value) => _onChanged();

    partial void OnFlickerChanged(bool value) => _onChanged();

    [RelayCommand]
    private void Remove() => _onRemove(this);

    private static int ParseInt(string? value, int min, int max) =>
        int.TryParse(value, out var parsed) ? Math.Clamp(parsed, min, max) : min;
}
