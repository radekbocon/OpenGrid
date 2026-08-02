using OpenGrid.Models;
using OpenGrid.Models.Telemetry;

namespace OpenGrid.Services.Sound;

public static class TelemetryInputResolver
{
    public static double GetNormalized(BassShakerInputSettings settings, TelemetryRecord telemetry)
    {
        var (value, referenceMax) = GetValueAndReference(settings.Input, telemetry);
        var min = referenceMax * settings.MinRpmPercent / 100.0;
        var max = referenceMax * settings.MaxRpmPercent / 100.0;
        var range = max - min;
        return range > 0
            ? Math.Clamp((value - min) / range, 0, 1)
            : 0;
    }

    private static (double Value, double ReferenceMax) GetValueAndReference(TelemetryInput input, TelemetryRecord telemetry)
    {
        return input switch
        {
            TelemetryInput.EngineRpm => (telemetry.EngineRpm, Math.Max(telemetry.MaxRpm, telemetry.EngineRpm)),
            _ => (0, 0),
        };
    }
}
