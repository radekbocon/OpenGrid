using OpenGrid.Models;

namespace OpenGrid.Models.Telemetry;

/// <summary>
/// Helpers for deriving a consistent wheel slip ratio across all supported games.
/// A slip ratio is positive when a wheel spins faster than the car and negative when it locks.
/// </summary>
public static class WheelSlipCalculator
{
    /// <summary>
    /// Fallback tyre radius used when a game does not expose a per-wheel radius.
    /// </summary>
    public const float DefaultTyreRadiusMeters = 0.33f;

    private const float MinimumReferenceSpeed = 1.0f;

    public static TireStats ToWheelSpeeds(TireStats angularSpeeds, TireStats tyreRadii)
    {
        return new TireStats(
            angularSpeeds.FrontLeft * ResolveRadius(tyreRadii.FrontLeft),
            angularSpeeds.FrontRight * ResolveRadius(tyreRadii.FrontRight),
            angularSpeeds.RearLeft * ResolveRadius(tyreRadii.RearLeft),
            angularSpeeds.RearRight * ResolveRadius(tyreRadii.RearRight));
    }

    public static TireStats ToWheelSpeeds(TireStats angularSpeeds, float tyreRadius)
    {
        var radius = ResolveRadius(tyreRadius);
        return new TireStats(
            angularSpeeds.FrontLeft * radius,
            angularSpeeds.FrontRight * radius,
            angularSpeeds.RearLeft * radius,
            angularSpeeds.RearRight * radius);
    }

    public static TireStats CalculateSlipRatios(TireStats wheelSpeeds, float vehicleSpeed)
    {
        var referenceSpeed = Math.Max(Math.Abs(vehicleSpeed), MinimumReferenceSpeed);
        var direction = vehicleSpeed < 0 ? -1.0f : 1.0f;
        return new TireStats(
            (wheelSpeeds.FrontLeft - vehicleSpeed) * direction / referenceSpeed,
            (wheelSpeeds.FrontRight - vehicleSpeed) * direction / referenceSpeed,
            (wheelSpeeds.RearLeft - vehicleSpeed) * direction / referenceSpeed,
            (wheelSpeeds.RearRight - vehicleSpeed) * direction / referenceSpeed);
    }

    public static float GetWheelLock(TireStats slipRatios, WheelSelection wheels)
    {
        return GetMaxWheelIntensity(slipRatios, wheels, static ratio => Math.Clamp(-ratio, 0, 1));
    }

    public static float GetWheelSpin(TireStats slipRatios, WheelSelection wheels)
    {
        return GetMaxWheelIntensity(slipRatios, wheels, static ratio => Math.Clamp(ratio, 0, 1));
    }

    private static float GetMaxWheelIntensity(TireStats slipRatios, WheelSelection wheels, Func<float, float> intensity)
    {
        var max = 0.0f;

        if (wheels.Includes(WheelSelection.FrontLeft))
        {
            max = Math.Max(max, intensity(slipRatios.FrontLeft));
        }

        if (wheels.Includes(WheelSelection.FrontRight))
        {
            max = Math.Max(max, intensity(slipRatios.FrontRight));
        }

        if (wheels.Includes(WheelSelection.RearLeft))
        {
            max = Math.Max(max, intensity(slipRatios.RearLeft));
        }

        if (wheels.Includes(WheelSelection.RearRight))
        {
            max = Math.Max(max, intensity(slipRatios.RearRight));
        }

        return max;
    }

    private static float ResolveRadius(float radius) =>
        radius > 0 && float.IsFinite(radius) ? radius : DefaultTyreRadiusMeters;
}
