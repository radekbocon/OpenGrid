using SimLab.Converters;
using SimLab.Models;
using SimLab.Models.Telemetry;
using SkiaSharp;

namespace SimLab.ViewModels;

public record LapComparisonItem
{
    public Lap Lap { get; }
    public SessionInfo SessionInfo { get; }
    public int ColorIndex { get; }
    public SKColor Color => ColorPalette.GetColor(ColorIndex);

    public string DisplayName => $"{SessionInfo.Car} - Lap {Lap.Number} ({LapTimeConverter.Format(Lap.Time)})";
    public string ShortName => $"Lap {Lap.Number}";

    public LapComparisonItem(Lap lap, SessionInfo sessionInfo, int colorIndex)
    {
        Lap = lap;
        SessionInfo = sessionInfo;
        ColorIndex = colorIndex;
    }
}
