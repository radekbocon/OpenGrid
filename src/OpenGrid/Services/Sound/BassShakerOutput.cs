using OpenGrid.Models;
using OpenGrid.Models.Telemetry;
using PortAudioSharp;
using Serilog;
using PortAudioStream = PortAudioSharp.Stream;

namespace OpenGrid.Services.Sound;

internal sealed class BassShakerOutput : IDisposable
{
    private const uint FramesPerBuffer = 256;
    private const double SmoothingTimeConstantSeconds = 0.004;
    private const double TwoPi = Math.PI * 2;

    private readonly PortAudioStream _stream;
    private readonly int _channelCount;
    private readonly double _sampleRate;
    private readonly double _smoothingAlpha;
    private volatile InputChannel[] _channels;
    private IReadOnlyList<BassShakerInputSettings> _inputSettings;

    public BassShakerOutput(int deviceIndex, IReadOnlyList<BassShakerInputSettings> inputs)
    {
        var deviceInfo = PortAudio.GetDeviceInfo(deviceIndex);
        _sampleRate = deviceInfo.defaultSampleRate;
        _channelCount = Math.Clamp(deviceInfo.maxOutputChannels, 1, 2);
        _smoothingAlpha = 1 - Math.Exp(-1 / (SmoothingTimeConstantSeconds * _sampleRate));
        _channels = inputs.Select(x => new InputChannel(x)).ToArray();
        _inputSettings = inputs;

        var outputParameters = new StreamParameters
        {
            device = deviceIndex,
            channelCount = _channelCount,
            sampleFormat = SampleFormat.Float32,
            suggestedLatency = deviceInfo.defaultLowOutputLatency,
            hostApiSpecificStreamInfo = IntPtr.Zero,
        };

        _stream = new PortAudioStream(null, outputParameters, _sampleRate, FramesPerBuffer, StreamFlags.NoFlag, OnAudioCallback, this);
        _stream.Start();
    }

    public void Reconfigure(IReadOnlyList<BassShakerInputSettings> inputs)
    {
        _channels = inputs.Select(x => new InputChannel(x)).ToArray();
        _inputSettings = inputs;
    }

    public bool HasInputs(IReadOnlyList<BassShakerInputSettings> inputs)
    {
        var current = _inputSettings;
        return current.Count == inputs.Count
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

    private unsafe StreamCallbackResult OnAudioCallback(
        IntPtr input,
        IntPtr output,
        uint frameCount,
        ref StreamCallbackTimeInfo timeInfo,
        StreamCallbackFlags statusFlags,
        IntPtr userDataPtr)
    {
        var channels = _channels;
        var outputSamples = (float*)output;

        for (var frame = 0u; frame < frameCount; frame++)
        {
            var sample = 0.0;
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
                sample += channel.SmoothedGain * Math.Sin(channel.Phase);
            }

            var clampedSample = (float)Math.Clamp(sample, -1, 1);
            for (var channelIndex = 0; channelIndex < _channelCount; channelIndex++)
            {
                outputSamples[frame * _channelCount + channelIndex] = clampedSample;
            }
        }

        return StreamCallbackResult.Continue;
    }

    public void Dispose()
    {
        try
        {
            _stream.Stop();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to stop bass shaker stream");
        }

        try
        {
            _stream.Dispose();
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
