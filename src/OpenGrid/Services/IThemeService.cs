using System.Collections.Generic;
using OpenGrid.Models;

namespace OpenGrid.Services;

public interface IThemeService
{
    IReadOnlyList<ThemeInfo> Themes { get; }
    ThemeInfo? CurrentTheme { get; }
    void ApplyTheme(ThemeInfo theme);
    ThemeInfo? GetThemeByName(string name);
}
