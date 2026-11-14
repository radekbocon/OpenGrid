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
            // Reversing makes the wheel speeds oppose the vehicle speed, which looks like a lock.
            if (settings.Input == TelemetryInput.WheelLock && IsGoingBackwards(telemetry))
            {
                return (0, 1);
            }

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

    private static bool IsGoingBackwards(TelemetryRecord telemetry)
    {
        if (telemetry.CurrentGear == Gear.R || telemetry.SpeedKmh < 0)
        {
            return true;
        }

        var speedMps = Math.Abs(telemetry.SpeedKmh) / 3.6f;
        if (speedMps < 0.5f)
        {
            return false;
        }

        var wheels = telemetry.WheelSpeeds;
        var averageWheelSpeed = (wheels.FrontLeft + wheels.FrontRight + wheels.RearLeft + wheels.RearRight) / 4f;
        return averageWheelSpeed < -0.2f;
    }
}
