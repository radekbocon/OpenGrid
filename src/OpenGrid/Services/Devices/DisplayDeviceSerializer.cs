using System.Text.Json;
using OpenGrid.Models;

namespace OpenGrid.Services.Devices;

public class DisplayDeviceSerializer : IDeviceSerializer
{
    public IDevice? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        
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
                DashboardId = dto.DashboardId,
                DashboardTrigger = dto.DashboardTrigger,
            }
        };
    }

    public string ToJson(IDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        var displayDevice = device as DisplayDevice;

        var dto = new DisplayDeviceDto
        {
            Id = device.Id,
            DeviceType = device.DeviceType,
            Name = device.Name,
            Description = device.Description,
            IsEnabled = device.IsEnabled,
            DashboardId = displayDevice?.DashboardId,
            DashboardTrigger = displayDevice?.DashboardTrigger ?? DashboardLaunchTrigger.None,
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
        public string? DashboardId { get; init; }
        public DashboardLaunchTrigger DashboardTrigger { get; init; }
    }
}