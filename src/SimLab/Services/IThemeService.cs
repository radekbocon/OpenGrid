using System.Collections.Generic;
using SimLab.Models;

namespace SimLab.Services;

public interface IThemeService
{
    IReadOnlyList<ThemeInfo> Themes { get; }
    ThemeInfo? CurrentTheme { get; }
    void ApplyTheme(ThemeInfo theme);
    ThemeInfo? GetThemeByName(string name);
}
