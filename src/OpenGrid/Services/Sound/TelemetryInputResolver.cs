using OpenGrid.Models;
using OpenGrid.Models.Telemetry;

namespace OpenGrid.Services.Sound;

public static class TelemetryInputResolver
{
    public static double GetNormalized(BassShakerInputSettings settings, TelemetryRecord telemetry)
    {
        var (value, referenceMax) = GetValueAndReference(settings, telemetry);
        var min = referenceMax * settings.MinPercent / 100.0;
        var max = referenceMax * settings.MaxPercent / 100.0;
        var range = max - min;
        return range > 0
            ? Math.Clamp((value - min) / range, 0, 1)
            : 0;
    }

    private static (double Value, double ReferenceMax) GetValueAndReference(BassShakerInputSettings settings, TelemetryRecord telemetry)
    {
        if (settings.Input is TelemetryInput.WheelLock or TelemetryInput.WheelSpin)
        {
            var vehicleSpeed = telemetry.SpeedKmh / 3.6f;
            var slipRatios = WheelSlipCalculator.CalculateSlipRatios(telemetry.WheelSpeeds, vehicleSpeed);
            var value = settings.Input == TelemetryInput.WheelLock
                ? WheelSlipCalculator.GetWheelLock(slipRatios, settings.Wheels)
                : WheelSlipCalculator.GetWheelSpin(slipRatios, settings.Wheels);
            return (value, 1);
        }

        return settings.Input switch
        {
            TelemetryInput.Abs => (telemetry.Abs, 1),
            TelemetryInput.Tc => (telemetry.Tc, 1),
            _ => (0, 0),
        };
    }
}
