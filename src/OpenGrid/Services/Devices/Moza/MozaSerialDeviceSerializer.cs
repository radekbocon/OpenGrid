using System.Text.Json;
using OpenGrid.Models;

namespace OpenGrid.Services.Devices.Moza;

public class MozaSerialDeviceSerializer : IDeviceSerializer
{
    public IDevice? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var dto = JsonSerializer.Deserialize<MozaSerialDeviceDto>(json);

        return dto switch
        {
            null => null,
            _ => new MozaSerialDevice
            {
                Id = dto.Id,
                DeviceType = DeviceType.MozaSerial,
                Name = dto.Name,
                Description = dto.Description,
                IsEnabled = dto.IsEnabled,
                PortName = dto.PortName ?? dto.Id,
                SerialNumber = dto.SerialNumber,
                VendorId = dto.VendorId,
                ProductId = dto.ProductId,
                Category = dto.Category,
                DriveLedsWithTelemetry = dto.DriveLedsWithTelemetry ?? true,
                RpmLedCount = dto.RpmLedCount is > 0 ? dto.RpmLedCount.Value : 10,
                DefaultLedPresetId = dto.DefaultLedPresetId,
                LedOverrides = dto.LedOverrides ?? [],
            }
        };
    }

    public string ToJson(IDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        var mozaDevice = device as MozaSerialDevice;

        var dto = new MozaSerialDeviceDto
        {
            Id = device.Id,
            DeviceType = DeviceType.MozaSerial,
            Name = device.Name,
            Description = device.Description,
            IsEnabled = device.IsEnabled,
            PortName = (device as ISerialDevice)?.PortName,
            SerialNumber = (device as ISerialDevice)?.SerialNumber,
            VendorId = (device as ISerialDevice)?.VendorId ?? 0,
            ProductId = (device as ISerialDevice)?.ProductId ?? 0,
            Category = mozaDevice?.Category ?? MozaDeviceCategory.Unknown,
            DriveLedsWithTelemetry = mozaDevice?.DriveLedsWithTelemetry ?? true,
            RpmLedCount = mozaDevice?.RpmLedCount ?? 10,
            DefaultLedPresetId = mozaDevice?.DefaultLedPresetId,
            LedOverrides = mozaDevice?.LedOverrides ?? [],
        };

        return JsonSerializer.Serialize(dto);
    }

    private class MozaSerialDeviceDto
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
        public string? DefaultLedPresetId { get; init; }
        public List<LedPresetOverride>? LedOverrides { get; init; }
    }
}
