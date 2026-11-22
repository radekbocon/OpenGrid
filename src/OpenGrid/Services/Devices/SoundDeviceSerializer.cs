using System.Text.Json;
using OpenGrid.Models;

namespace OpenGrid.Services.Devices;

public class SoundDeviceSerializer : IDeviceSerializer
{
    public IDevice? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var dto = JsonSerializer.Deserialize<SoundDeviceDto>(json);

        return dto switch
        {
            null => null,
            _ => new SoundDevice
            {
                Id = dto.Id,
                DeviceType = dto.DeviceType,
                Name = dto.Name,
                Description = dto.Description,
                IsEnabled = dto.IsEnabled,
                DefaultBassShakerPresetId = dto.DefaultBassShakerPresetId,
                BassShakerOverrides = dto.BassShakerOverrides ?? [],
                BassShaker = dto.BassShaker,
            }
        };
    }

    public string ToJson(IDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        var soundDevice = device as SoundDevice;

        var dto = new SoundDeviceDto
        {
            Id = device.Id,
            DeviceType = device.DeviceType,
            Name = device.Name,
            Description = device.Description,
            IsEnabled = device.IsEnabled,
            DefaultBassShakerPresetId = soundDevice?.DefaultBassShakerPresetId,
            BassShakerOverrides = soundDevice?.BassShakerOverrides ?? [],
            BassShaker = soundDevice?.BassShaker,
        };

        return JsonSerializer.Serialize(dto);
    }

    private class SoundDeviceDto
    {
        public required string Id { get; init; }
        public required DeviceType DeviceType { get; init; }
        public required string Name { get; init; }
        public string? Description { get; init; }
        public required bool IsEnabled { get; init; }
        public string? DefaultBassShakerPresetId { get; init; }
        public List<BassShakerPresetOverride>? BassShakerOverrides { get; init; }
        public BassShakerConfiguration? BassShaker { get; init; }
    }
}
