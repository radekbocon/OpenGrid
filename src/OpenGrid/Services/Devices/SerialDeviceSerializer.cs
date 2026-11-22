using System.Text.Json;
using OpenGrid.Models;

namespace OpenGrid.Services.Devices;

public class SerialDeviceSerializer : IDeviceSerializer
{
    public IDevice? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var dto = JsonSerializer.Deserialize<SerialDeviceDto>(json);

        return dto switch
        {
            null => null,
            _ => new SerialDevice
            {
                Id = dto.Id,
                DeviceType = DeviceType.Serial,
                Name = dto.Name,
                Description = dto.Description,
                IsEnabled = dto.IsEnabled,
                PortName = dto.PortName ?? dto.Id,
                SerialNumber = dto.SerialNumber,
                VendorId = dto.VendorId,
                ProductId = dto.ProductId,
                Category = dto.Category,
                DriveLedsWithTelemetry = dto.DriveLedsWithTelemetry ?? true,
                RpmLedCount = dto.RpmLedCount is > 0 and <= 32 ? dto.RpmLedCount.Value : 10,
            }
        };
    }

    public string ToJson(IDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        var serialDevice = device as SerialDevice;

        var dto = new SerialDeviceDto
        {
            Id = device.Id,
            DeviceType = DeviceType.Serial,
            Name = device.Name,
            Description = device.Description,
            IsEnabled = device.IsEnabled,
            PortName = serialDevice?.PortName,
            SerialNumber = serialDevice?.SerialNumber,
            VendorId = serialDevice?.VendorId ?? 0,
            ProductId = serialDevice?.ProductId ?? 0,
            Category = serialDevice?.Category ?? MozaDeviceCategory.Unknown,
            DriveLedsWithTelemetry = serialDevice?.DriveLedsWithTelemetry ?? true,
            RpmLedCount = serialDevice?.RpmLedCount ?? 10,
        };

        return JsonSerializer.Serialize(dto);
    }

    private class SerialDeviceDto
    {
        public required string Id { get; init; }
        public DeviceType DeviceType { get; init; }
        public required string Name { get; init; }
        public string? Description { get; init; }
        public bool IsEnabled { get; init; }
        public string? PortName { get; init; }
        public string? SerialNumber { get; init; }
        public ushort VendorId { get; init; }
        public ushort ProductId { get; init; }
        public MozaDeviceCategory Category { get; init; }
        public bool? DriveLedsWithTelemetry { get; init; }
        public int? RpmLedCount { get; init; }
    }
}
