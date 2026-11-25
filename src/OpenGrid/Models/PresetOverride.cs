namespace OpenGrid.Models;

/// <summary>
/// Common game/car targeting for device presets. Concrete presets derive from
/// this so the matching logic and the override UI can be shared.
/// </summary>
public abstract class PresetOverride
{
    /// <summary>Steam application id of the game this override applies to. Null matches any game.</summary>
    public int? GameAppId { get; set; }

    /// <summary>Telemetry car this override applies to. Null matches any car.</summary>
    public CarProfile? Car { get; set; }

    public string PresetId { get; set; } = string.Empty;
}

public static class PresetOverrideExtensions
{
    public static bool Matches(this PresetOverride overrideSettings, int? gameAppId, string? carName)
    {
        var hasGame = overrideSettings.GameAppId is not null;
        var hasCar = overrideSettings.Car is not null;

        if (!hasGame && !hasCar)
        {
            return false;
        }

        if (overrideSettings.GameAppId is { } requiredGameAppId &&
            (!gameAppId.HasValue || requiredGameAppId != gameAppId.Value))
        {
            return false;
        }

        return !hasCar ||
               (carName is not null &&
                string.Equals(overrideSettings.Car?.Name, carName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Game + car beats game only, which beats car only.
    /// </summary>
    public static int GetSpecificity(this PresetOverride overrideSettings)
    {
        var score = overrideSettings.GameAppId is not null ? 2 : 0;
        if (overrideSettings.Car is not null)
        {
            score++;
        }

        return score;
    }

    public static TPreset? ResolvePreset<TPreset>(
        this IEnumerable<PresetOverride> overrides,
        string? defaultPresetId,
        int? gameAppId,
        string? carName,
        Func<string?, TPreset?> getById)
        where TPreset : class
    {
        var matchingOverride = overrides
            .Where(overrideSettings => overrideSettings.Matches(gameAppId, carName))
            .OrderByDescending(static overrideSettings => overrideSettings.GetSpecificity())
            .Select(overrideSettings => getById(overrideSettings.PresetId))
            .FirstOrDefault(static preset => preset is not null);

        return matchingOverride ?? getById(defaultPresetId);
    }
}
