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
            if (info.maxOutputChannels <= 0)
                continue;

            devices.Add(new SoundDeviceInfo
            {
                Id = index.ToString(),
                Name = info.name,
                Description = $"Output, {info.maxOutputChannels} channel(s), {info.defaultSampleRate:0.##} Hz",
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
