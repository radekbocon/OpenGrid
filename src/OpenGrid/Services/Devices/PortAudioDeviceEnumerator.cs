using System.Diagnostics.CodeAnalysis;
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
            if ((PaHostApiTypeId)info.hostApi == PaHostApiTypeId.PaInDevelopment)
                continue;
            
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

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public enum PaHostApiTypeId
{
    PaInDevelopment = 0, /* use while developing support for a new host API */
    PaDirectSound = 1,
    PaMme = 2,
    PaAsio = 3,
    PaSoundManager = 4,
    PaCoreAudio = 5,
    PaOss = 7,
    PaAlsa = 8,
    PaAl = 9,
    PaBeOs = 10,
    PaWdmks = 11,
    PaJack = 12,
    PaWasapi = 13,
    PaAudioScienceHpi = 14,
    PaAudioIo = 15,
    PaPulseAudio = 16,
    PaSndio = 17,
}
