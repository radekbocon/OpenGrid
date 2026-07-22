using System.Diagnostics;
using System.Text.Json;
using Serilog;

namespace OpenGrid.Services.Devices;

internal sealed class PipeWireDeviceEnumerator
{
    public List<SoundDeviceInfo> EnumerateDevices()
    {
        try
        {
            return EnumerateDevicesInternal();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to enumerate PipeWire devices");
            return [];
        }
    }

    private static List<SoundDeviceInfo> EnumerateDevicesInternal()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "pw-dump",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            Log.Warning("Failed to start pw-dump process");
            return [];
        }

        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit(TimeSpan.FromSeconds(5));

        if (process.ExitCode != 0)
        {
            Log.Warning("pw-dump exited with code {ExitCode}", process.ExitCode);
            return [];
        }

        return ParsePwDump(output);
    }
    
    //"props" : {
    //   "alsa.card" : 2,
    //   "alsa.card_name" : "Fast Track",
    //   "alsa.class" : "generic",
    //   "alsa.components" : "USB0763:2024",
    //   "alsa.device" : 0,
    //   "alsa.driver_name" : "snd_usb_audio",
    //   "alsa.id" : "Track",
    //   "alsa.long_card_name" : "M-Audio Fast Track at usb-0000:0e:00.3-2.1, full speed",
    //   "alsa.mixer_name" : "USB Mixer",
    //   "alsa.name" : "USB Audio",
    //   "alsa.resolution_bits" : 16,
    //   "alsa.subclass" : "generic-mix",
    //   "alsa.subdevice" : 0,
    //   "alsa.subdevice_name" : "subdevice #0",
    //   "alsa.sync.id" : "00000000:00000000:00000000:00000000",
    //   "api.alsa.card.longname" : "M-Audio Fast Track at usb-0000:0e:00.3-2.1, full speed",
    //   "api.alsa.card.name" : "Fast Track",
    //   "api.alsa.path" : "front:2",
    //   "api.alsa.pcm.card" : 2,
    //   "api.alsa.pcm.stream" : "capture",
    //   "audio.channels" : 2,
    //   "audio.position" : "[ FL, FR ]",
    //   "card.profile.device" : 0,
    //   "client.id" : 46,
    //   "clock.quantum-limit" : 8192,
    //   "device.api" : "alsa",
    //   "device.bus" : "usb",
    //   "device.class" : "sound",
    //   "device.icon-name" : "audio-card-analog",
    //   "device.id" : 55,
    //   "device.profile.description" : "Analogowe stereo",
    //   "device.profile.name" : "analog-stereo",
    //   "device.routes" : 1,
    //   "factory.id" : 19,
    //   "factory.name" : "api.alsa.pcm.source",
    //   "library.name" : "audioconvert/libspa-audioconvert",
    //   "media.class" : "Audio/Source",
    //   "node.description" : "M-Audio Fast Track MKII Analogowe stereo",
    //   "node.driver" : true,
    //   "node.loop.name" : "data-loop.0",
    //   "node.name" : "alsa_input.usb-M-Audio_Fast_Track-00.analog-stereo",
    //   "node.nick" : "Fast Track",
    //   "node.pause-on-idle" : false,
    //   "object.id" : 60,
    //   "object.path" : "alsa:acp:Track:0:capture",
    //   "object.serial" : 65,
    //   "port.group" : "capture",
    //   "priority.driver" : 2109,
    //   "priority.session" : 2109
    // },

    private static List<SoundDeviceInfo> ParsePwDump(string json)
    {
        var devices = new List<SoundDeviceInfo>();

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return devices;

        foreach (var element in doc.RootElement.EnumerateArray())
        {
            if (!element.TryGetProperty("type", out var typeProp))
                continue;

            var type = typeProp.GetString();
            if (type != "PipeWire:Interface:Node")
                continue;

            if (!element.TryGetProperty("info", out var info))
                continue;

            if (!info.TryGetProperty("props", out var props))
                continue;

            if (!props.TryGetProperty("media.class", out var mediaClassProp))
                continue;

            var mediaClass = mediaClassProp.GetString() ?? "";
            if (!mediaClass.Contains("Audio"))
                continue;

            var id = element.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : 0;
            var nick = props.TryGetProperty("node.nick", out var nickProp) ? nickProp.GetString() : null;
            var description = props.TryGetProperty("device.profile.description", out var descProp) ? descProp.GetString() : null;

            devices.Add(new SoundDeviceInfo
            {
                Id = id.ToString(),
                Name = nick ?? $"Audio Device {id}",
                Description = description,
                PipeWireNodeId = (uint)id,
                MediaClass = mediaClass,
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
    public uint PipeWireNodeId { get; init; }
    public string? MediaClass { get; init; }
}
