using SimLab.Models;
using SkiaSharp;

namespace SimLab.ViewModels;

public record LapComparisonItem
{
    public Lap Lap { get; }
    public SessionInfo SessionInfo { get; }
    public int ColorIndex { get; }
    public SKColor Color => ColorPalette.GetColor(ColorIndex);

    public string DisplayName => $"{SessionInfo.Car} - Lap {Lap.Number} ({Lap.Time:mm\\:ss\\.fff})";
    public string ShortName => $"Lap {Lap.Number}";

    public LapComparisonItem(Lap lap, SessionInfo sessionInfo, int colorIndex)
    {
        Lap = lap;
        SessionInfo = sessionInfo;
        ColorIndex = colorIndex;
    }
}
