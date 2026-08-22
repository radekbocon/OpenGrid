using System.Runtime.InteropServices;
using OpenGrid.Models;
using OpenGrid.Models.Telemetry;
using SDL3;
using Serilog;

namespace OpenGrid.Services.Sound;

internal sealed class BassShakerOutput : IDisposable
{
    private const double SmoothingTimeConstantSeconds = 0.004;
    private const double TwoPi = Math.PI * 2;
    private const int DefaultSampleRate = 48000;

    private readonly SDL.AudioStreamCallback _audioCallback;
    private readonly int _channelCount;
    private readonly double _sampleRate;
    private readonly double _smoothingAlpha;
    private IntPtr _stream;
    private float[] _buffer = [];
    private volatile InputChannel[] _channels;
    private double _deviceVolume;
    private IReadOnlyList<BassShakerInputSettings> _inputSettings;

    public BassShakerOutput(string deviceId, double deviceVolume, IReadOnlyList<BassShakerInputSettings> inputs)
    {
        var deviceInstance = ResolveDeviceInstance(deviceId);

        if (!SDL.GetAudioDeviceFormat(deviceInstance, out var deviceSpec, out _))
        {
            throw new InvalidOperationException($"Failed to query audio device format for '{deviceId}': {SDL.GetError()}");
        }

        _sampleRate = deviceSpec.Freq > 0 ? deviceSpec.Freq : DefaultSampleRate;
        _channelCount = Math.Clamp(deviceSpec.Channels, 1, 2);
        _smoothingAlpha = 1 - Math.Exp(-1 / (SmoothingTimeConstantSeconds * _sampleRate));
        Volatile.Write(ref _deviceVolume, deviceVolume);
        _channels = inputs.Select(x => new InputChannel(x)).ToArray();
        _inputSettings = inputs;
        _audioCallback = OnAudioCallback;

        var spec = new SDL.AudioSpec
        {
            Format = SDL.AudioFormat.AudioF32LE,
            Channels = _channelCount,
            Freq = (int)_sampleRate,
        };

        _stream = SDL.OpenAudioDeviceStream(deviceInstance, ref spec, _audioCallback, IntPtr.Zero);
        if (_stream == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Failed to open audio stream for '{deviceId}': {SDL.GetError()}");
        }

        if (!SDL.ResumeAudioStreamDevice(_stream))
        {
            var error = SDL.GetError();
            SDL.DestroyAudioStream(_stream);
            _stream = IntPtr.Zero;
            throw new InvalidOperationException($"Failed to start audio stream for '{deviceId}': {error}");
        }
    }

    public void Reconfigure(double deviceVolume, IReadOnlyList<BassShakerInputSettings> inputs)
    {
        Volatile.Write(ref _deviceVolume, deviceVolume);
        _channels = inputs.Select(x => new InputChannel(x)).ToArray();
        _inputSettings = inputs;
    }

    public bool HasInputs(double deviceVolume, IReadOnlyList<BassShakerInputSettings> inputs)
    {
        var current = _inputSettings;
        return _deviceVolume == deviceVolume
            && current.Count == inputs.Count
            && current.SequenceEqual(inputs, ReferenceEqualityComparer.Instance);
    }

    public void ApplyTelemetry(TelemetryRecord telemetry)
    {
        foreach (var channel in _channels)
        {
            var normalized = TelemetryInputResolver.GetNormalized(channel.Settings, telemetry);
            Volatile.Write(ref channel.Normalized, normalized);
        }
    }

    public void ClearTelemetry()
    {
        foreach (var channel in _channels)
        {
            Volatile.Write(ref channel.Normalized, 0);
        }
    }

    private void OnAudioCallback(IntPtr userdata, IntPtr stream, int additionalAmount, int totalAmount)
    {
        var bytesPerFrame = _channelCount * sizeof(float);
        var frameCount = additionalAmount / bytesPerFrame;
        if (frameCount <= 0)
        {
            return;
        }

        var sampleCount = frameCount * _channelCount;
        var buffer = _buffer.Length >= sampleCount ? _buffer : _buffer = new float[sampleCount];
        var channels = _channels;
        var deviceVolume = Volatile.Read(ref _deviceVolume);

        for (var frame = 0; frame < frameCount; frame++)
        {
            var leftSample = 0.0;
            var rightSample = 0.0;
            foreach (var channel in channels)
            {
                var targetGain = channel.Settings.IsEnabled
                    ? channel.Settings.Volume * Volatile.Read(ref channel.Normalized)
                    : 0;
                channel.SmoothedGain += (targetGain - channel.SmoothedGain) * _smoothingAlpha;
                channel.Phase += TwoPi * channel.Settings.Frequency / _sampleRate;
                if (channel.Phase > TwoPi)
                {
                    channel.Phase -= TwoPi;
                }

                var value = channel.SmoothedGain * Math.Sin(channel.Phase);
                switch (channel.Settings.Channel)
                {
                    case BassShakerChannel.Right:
                        rightSample += value;
                        break;
                    case BassShakerChannel.Left:
                        leftSample += value;
                        break;
                    default:
                        leftSample += value;
                        rightSample += value;
                        break;
                }
            }

            var frameIndex = frame * _channelCount;
            buffer[frameIndex] = (float)Math.Clamp(leftSample * deviceVolume, -1, 1);
            if (_channelCount == 2)
            {
                buffer[frameIndex + 1] = (float)Math.Clamp(rightSample * deviceVolume, -1, 1);
            }
        }

        SDL.PutAudioStreamData(stream, MemoryMarshal.Cast<float, byte>(buffer.AsSpan(0, sampleCount)), sampleCount * sizeof(float));
    }

    private static uint ResolveDeviceInstance(string deviceId)
    {
        var deviceIds = SDL.GetAudioPlaybackDevices(out var count) ?? [];
        foreach (var id in deviceIds)
        {
            if (!SDL.IsAudioDevicePhysical(id))
            {
                continue;
            }

            if (string.Equals(SDL.GetAudioDeviceName(id), deviceId, StringComparison.OrdinalIgnoreCase))
            {
                return id;
            }
        }

        throw new InvalidOperationException($"Audio output device '{deviceId}' not found");
    }

    public void Dispose()
    {
        var stream = Interlocked.Exchange(ref _stream, IntPtr.Zero);
        if (stream == IntPtr.Zero)
        {
            return;
        }

        try
        {
            SDL.PauseAudioStreamDevice(stream);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to stop bass shaker stream");
        }

        try
        {
            SDL.DestroyAudioStream(stream);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to dispose bass shaker stream");
        }
    }

    private sealed class InputChannel(BassShakerInputSettings settings)
    {
        public BassShakerInputSettings Settings { get; } = settings;
        public double Normalized;
        public double SmoothedGain;
        public double Phase;
    }
}
