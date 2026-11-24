using Avalonia.Media;
using OpenGrid.Models;
using OpenGrid.Models.Telemetry;

namespace OpenGrid.Services.Devices;

public sealed record RpmLedEffectSnapshot(
    RpmLedSegment? ActiveFrame,
    IReadOnlyList<Color> Leds,
    bool LightsOn,
    uint ActiveMask);

public interface IRpmLedEffectService
{
    RpmLedEffectSnapshot? Evaluate(IRpmLedTelemetryDevice device, TelemetryRecord? telemetry);
    void Reset(string deviceId);
}

/// <summary>
/// Computes generic RPM LED effect state from telemetry. Device-specific services
/// are responsible for transporting the resolved state to hardware.
/// </summary>
public sealed class RpmLedEffectService : IRpmLedEffectService
{
    private static readonly long FlickerIntervalTicks = TimeSpan.FromMilliseconds(200).Ticks;

    private readonly ICarConfigService _carConfigService;
    private readonly Dictionary<string, FlickerState> _flickerStates = [];
    private readonly Lock _sync = new();

    public RpmLedEffectService(ICarConfigService carConfigService)
    {
        _carConfigService = carConfigService;
    }

    public RpmLedEffectSnapshot? Evaluate(IRpmLedTelemetryDevice device, TelemetryRecord? telemetry)
    {
        ArgumentNullException.ThrowIfNull(device);

        if (telemetry is null)
        {
            return null;
        }

        var segments = device.GetNormalizedRpmSegments();
        var activeFrame = SelectActiveFrame(telemetry, segments);
        var leds = activeFrame?.Leds ?? Enumerable.Repeat(RpmLedSegment.OffLed, device.RpmLedCount).ToList();
        var lightsOn = ResolveLightsOn(device.Id, activeFrame);
        var activeMask = lightsOn ? RpmLedSegment.BuildMask(leds) : 0u;

        return new RpmLedEffectSnapshot(activeFrame, leds, lightsOn, activeMask);
    }

    public void Reset(string deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);

        lock (_sync)
        {
            _flickerStates.Remove(deviceId);
        }
    }

    private bool ResolveLightsOn(string deviceId, RpmLedSegment? frame)
    {
        lock (_sync)
        {
            if (!_flickerStates.TryGetValue(deviceId, out var state))
            {
                state = new FlickerState();
                _flickerStates[deviceId] = state;
            }

            var frameId = frame is null
                ? "off"
                : $"{frame.ThresholdPercent}:{frame.Flicker}:{string.Join(',', frame.Leds)}";

            if (frameId != state.FrameId)
            {
                state.FrameId = frameId;
                state.LightsOn = true;
                state.LastToggleTicks = DateTime.Now.Ticks;
            }

            if (frame?.Flicker != true)
            {
                state.LightsOn = true;
                return true;
            }

            var now = DateTime.Now.Ticks;
            if (now - state.LastToggleTicks >= FlickerIntervalTicks)
            {
                state.LightsOn = !state.LightsOn;
                state.LastToggleTicks = now;
            }

            return state.LightsOn;
        }
    }

    private RpmLedSegment? SelectActiveFrame(
        TelemetryRecord telemetry,
        IReadOnlyList<RpmLedSegment> segments)
    {
        var referenceRpm = GetReferenceRpm(telemetry);
        var fraction = Math.Clamp(telemetry.EngineRpm / Math.Max(referenceRpm, 1f), 0f, 1f) * 100f;

        RpmLedSegment? active = null;
        foreach (var segment in segments)
        {
            if (fraction < segment.ThresholdPercent)
            {
                break;
            }

            active = segment;
        }

        return active;
    }

    private float GetReferenceRpm(TelemetryRecord telemetry)
    {
        var profile = string.IsNullOrWhiteSpace(telemetry.Car.Name)
            ? null
            : _carConfigService.GetByCarName(telemetry.Car.Name);

        var maxRpm = telemetry.MaxRpm > 0
            ? telemetry.MaxRpm
            : profile?.MaxRpm ?? 8000;

        var redline = profile?.RedlineRpm ?? 0;
        return redline > 0 ? Math.Min(redline, maxRpm) : maxRpm;
    }

    private sealed class FlickerState
    {
        public string? FrameId { get; set; }
        public bool LightsOn { get; set; } = true;
        public long LastToggleTicks { get; set; }
    }
}
