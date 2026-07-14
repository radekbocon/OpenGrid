using System.Text.Json;
using OpenGrid.Models;

namespace OpenGrid.Services.Devices;

public class DisplayDeviceSerializer : IDeviceSerializer
{
    public IDevice? FromJson(string json)
    {
        var dto = JsonSerializer.Deserialize<DisplayDeviceDto>(json);

        return dto switch
        {
            null => null,
            _ => new DisplayDevice
            {
                Id = dto.Id,
                DeviceType = dto.DeviceType,
                Name = dto.Name,
                Description = dto.Description,
                IsEnabled = dto.IsEnabled,
            }
        };
    }

    public string ToJson(IDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        var dto = new DisplayDeviceDto
        {
            Id = device.Id,
            DeviceType = device.DeviceType,
            Name = device.Name,
            Description = device.Description,
            IsEnabled = device.IsEnabled,
        };

        return JsonSerializer.Serialize(dto);
    }
    
    private class DisplayDeviceDto
    {
        public required string Id { get; init; }
        public required DeviceType DeviceType { get; init; }
        public required string Name { get; init; }
        public string? Description { get; init; }
        public required bool IsEnabled { get; init; }
    }
}