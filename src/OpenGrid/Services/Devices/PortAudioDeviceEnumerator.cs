using PortAudioSharp;
using Serilog;

namespace OpenGrid.Services.Devices;

internal sealed class PortAudioDeviceEnumerator
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
        PortAudio.Initialize();
        try
        {
            return EnumeratePortAudioDevices();
        }
        finally
        {
            PortAudio.Terminate();
        }
    }

    private static List<SoundDeviceInfo> EnumeratePortAudioDevices()
    {
        var devices = new List<SoundDeviceInfo>();

        for (var index = 0; index < PortAudio.DeviceCount; index++)
        {
            var info = PortAudio.GetDeviceInfo(index);
            if (info is { maxInputChannels: <= 0, maxOutputChannels: <= 0 })
                continue;

            var direction = info is { maxInputChannels: > 0, maxOutputChannels: > 0 }
                ? "Input/Output"
                : info.maxInputChannels > 0
                    ? "Input"
                    : "Output";

            var channels = Math.Max(info.maxInputChannels, info.maxOutputChannels);

            devices.Add(new SoundDeviceInfo
            {
                Id = index.ToString(),
                Name = info.name,
                Description = $"{direction}, {channels} channel(s), {info.defaultSampleRate:0.##} Hz",
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
