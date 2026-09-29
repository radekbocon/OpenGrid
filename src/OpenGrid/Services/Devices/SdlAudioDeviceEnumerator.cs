using System.Diagnostics.CodeAnalysis;
using SDL3;
using Serilog;

namespace OpenGrid.Services.Devices;

internal static class SdlAudio
{
    private static readonly object Sync = new();
    private static bool _initialized;

    public static void EnsureInitialized()
    {
        lock (Sync)
        {
            if (_initialized)
            {
                return;
            }

            if (!SDL.InitSubSystem(SDL.InitFlags.Audio))
            {
                throw new InvalidOperationException($"Failed to initialize SDL audio: {SDL.GetError()}");
            }

            _initialized = true;
        }
    }
}

internal sealed class SdlAudioDeviceEnumerator
{
    public List<SoundDeviceInfo> EnumerateDevices()
    {
        try
        {
            return EnumerateDevicesInternal();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to enumerate audio devices");
            return [];
        }
    }

    private static List<SoundDeviceInfo> EnumerateDevicesInternal()
    {
        SdlAudio.EnsureInitialized();

        var devices = new List<SoundDeviceInfo>();
        var deviceIds = SDL.GetAudioPlaybackDevices(out var count) ?? [];

        foreach (var deviceId in deviceIds)
        {
            if (!SDL.IsAudioDevicePhysical(deviceId))
                continue;

            if (!SDL.GetAudioDeviceFormat(deviceId, out var spec, out _))
                continue;

            if (spec.Channels <= 0)
                continue;

            var name = SDL.GetAudioDeviceName(deviceId);
            if (string.IsNullOrEmpty(name))
                continue;

            devices.Add(new SoundDeviceInfo
            {
                Id = name,
                Name = name,
                Description = $"Output, {spec.Channels} channel(s), {spec.Freq:0.##} Hz",
            });
        }

        return devices;
    }
}

internal sealed class SoundDeviceInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
}
