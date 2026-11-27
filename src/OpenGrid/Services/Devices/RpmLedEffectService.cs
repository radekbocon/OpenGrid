using Avalonia.Media;
using OpenGrid.Models;
using OpenGrid.Models.Telemetry;

namespace OpenGrid.Services.Devices;

public sealed record RpmLedEffectSnapshot(
    IReadOnlyList<Color> Leds,
    bool LightsOn);

public interface IRpmLedEffectService : IDisposable
{
    RpmLedEffectSnapshot? Evaluate(
        IRpmLedTelemetryDevice device,
        TelemetryRecord? telemetry,
        SteamGame? game = null);

    /// <summary>First frame of the default preset, used when opening a connection.</summary>
    IReadOnlyList<Color>? GetInitialLeds(IRpmLedTelemetryDevice device);

    void Reset(string deviceId);
}

/// <summary>
/// Computes generic LED effect state from telemetry. The active preset is
/// resolved per device from its default preset and game/car overrides, using
/// the same rules as bass shakers. Enabled effects are applied in list order:
/// later effects override LEDs set by earlier ones, and black LEDs are left
/// untouched so lower-priority effects can show through. Device-specific
/// services only transport the resolved state to hardware.
/// </summary>
public sealed class RpmLedEffectService : IRpmLedEffectService
{
    private static readonly long FlickerIntervalTicks = TimeSpan.FromMilliseconds(200).Ticks;

    private readonly ICarConfigService _carConfigService;
    private readonly IDeviceService _deviceService;
    private readonly ILedPresetService _presetService;
    private readonly Dictionary<string, FlickerState> _flickerStates = [];
    private readonly Lock _sync = new();
    private bool _disposed;

    public RpmLedEffectService(
        ICarConfigService carConfigService,
        IDeviceService deviceService,
        ILedPresetService presetService)
    {
        _carConfigService = carConfigService;
        _deviceService = deviceService;
        _presetService = presetService;
        _presetService.PresetRemoved += OnPresetRemoved;
    }

    public RpmLedEffectSnapshot? Evaluate(
        IRpmLedTelemetryDevice device,
        TelemetryRecord? telemetry,
        SteamGame? game = null)
    {
        ArgumentNullException.ThrowIfNull(device);

        if (telemetry is null)
        {
            return null;
        }

        var preset = device.LedOverrides.ResolvePreset(
            device.DefaultLedPresetId,
            game?.AppId,
            telemetry.Car.Name,
            _presetService.GetById);

        var (leds, flickerFrame) = ResolveLeds(preset, telemetry, device.RpmLedCount);
        var lightsOn = ResolveLightsOn(device.Id, flickerFrame);

        return new RpmLedEffectSnapshot(leds, lightsOn);
    }

    public IReadOnlyList<Color>? GetInitialLeds(IRpmLedTelemetryDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        var preset = device.LedOverrides.ResolvePreset(
            device.DefaultLedPresetId,
            null,
            null,
            _presetService.GetById);

        var leds = Enumerable.Repeat(RpmLedSegment.OffLed, device.RpmLedCount).ToList();
        var hasEnabledEffect = false;

        foreach (var effect in preset?.Configuration.Effects ?? [])
        {
            if (effect is not { IsEnabled: true, Type: LedEffectType.Rpm })
            {
                continue;
            }

            hasEnabledEffect = true;
            var firstFrame = RpmLedSegment.Normalize(effect.RpmSegments, device.RpmLedCount)
                .FirstOrDefault();
            if (firstFrame is not null)
            {
                OverlayFrame(leds, firstFrame.Leds);
            }
        }

        return hasEnabledEffect ? leds : null;
    }

    public void Reset(string deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);

        lock (_sync)
        {
            _flickerStates.Remove(deviceId);
        }
    }

    private void OnPresetRemoved(object? sender, LedPreset preset)
    {
        foreach (var device in _deviceService.GetPersistedDevices().OfType<IRpmLedTelemetryDevice>())
        {
            var deviceChanged = false;

            if (device.DefaultLedPresetId == preset.Id)
            {
                device.DefaultLedPresetId = null;
                deviceChanged = true;
            }

            if (device.LedOverrides.RemoveAll(x => x.PresetId == preset.Id) > 0)
            {
                deviceChanged = true;
            }

            if (!deviceChanged)
            {
                continue;
            }

            _deviceService.SaveDevice(device);
            Reset(device.Id);
        }
    }

    private (List<Color> Leds, RpmLedSegment? FlickerFrame) ResolveLeds(
        LedPreset? preset,
        TelemetryRecord telemetry,
        int ledCount)
    {
        var leds = Enumerable.Repeat(RpmLedSegment.OffLed, ledCount).ToList();
        RpmLedSegment? flickerFrame = null;

        foreach (var effect in preset?.Configuration.Effects ?? [])
        {
            if (effect is not { IsEnabled: true, Type: LedEffectType.Rpm })
            {
                continue;
            }

            var segments = RpmLedSegment.Normalize(effect.RpmSegments, ledCount);
            var frame = SelectActiveFrame(telemetry, segments);
            if (frame is null)
            {
                continue;
            }

            OverlayFrame(leds, frame.Leds);
            if (flickerFrame is null && frame.Flicker)
            {
                flickerFrame = frame;
            }
        }

        return (leds, flickerFrame);
    }

    private static void OverlayFrame(IList<Color> leds, IReadOnlyList<Color> frame)
    {
        for (var i = 0; i < leds.Count && i < frame.Count; i++)
        {
            // Black is treated as transparent when effects are layered, so an
            // LED not set by a later effect keeps the earlier effect's colour.
            if (frame[i] != RpmLedSegment.OffLed)
            {
                leds[i] = frame[i];
            }
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

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _presetService.PresetRemoved -= OnPresetRemoved;

        lock (_sync)
        {
            _flickerStates.Clear();
        }
    }

    private sealed class FlickerState
    {
        public string? FrameId { get; set; }
        public bool LightsOn { get; set; } = true;
        public long LastToggleTicks { get; set; }
    }
}
