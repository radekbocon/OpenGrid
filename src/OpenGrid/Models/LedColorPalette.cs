using Avalonia.Controls;
using Avalonia.Media;

namespace OpenGrid.Models;

public sealed class LedColorPalette : IColorPalette
{
    // Named palette entries used by built-in presets as well as the picker.
    public static readonly Color Green = Color.Parse("#FF00FF00");
    public static readonly Color Amber = Color.Parse("#FFFFC800");
    public static readonly Color Red = Color.Parse("#FFFF0000");

    private static readonly Color[,] Palette =
    {
        {
            Colors.Black, Color.Parse("#FF003B00"), Color.Parse("#FF3D4B00"), Color.Parse("#FF4D3300"),
            Color.Parse("#FF5C0000"), Color.Parse("#FF004D59"), Color.Parse("#FF001F66"), Color.Parse("#FF4D0059")
        },
        {
            Color.Parse("#FF333333"), Color.Parse("#FF008000"), Color.Parse("#FF80B000"), Color.Parse("#FFA66B00"),
            Color.Parse("#FFB30000"), Color.Parse("#FF008C9E"), Color.Parse("#FF0043C4"), Color.Parse("#FF9800B3")
        },
        {
            Color.Parse("#FF888888"), Color.Parse("#FF00D000"), Color.Parse("#FFB5FF00"), Color.Parse("#FFFFA500"),
            Color.Parse("#FFFF3B00"), Color.Parse("#FF00D0E0"), Color.Parse("#FF1E75FF"), Color.Parse("#FFE000FF")
        },
        {
            Colors.White, Green, Color.Parse("#FFD4FF33"), Amber,
            Red, Color.Parse("#FF00FFFF"), Color.Parse("#FF007BFF"), Color.Parse("#FFFF4DFF")
        },
    };

    public int ColorCount => Palette.GetLength(1);

    public int ShadeCount => Palette.GetLength(0);

    public Color GetColor(int colorIndex, int shadeIndex) => Palette[shadeIndex, colorIndex];
}