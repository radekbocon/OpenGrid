using System.Text.Json;
using OpenGrid.Models;

namespace OpenGrid.Services.Devices;

public class SerialDeviceSerializer : IDeviceSerializer
{
    public IDevice? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var dto = JsonSerializer.Deserialize<SerialDeviceDto>(json);
        var totalLedCount = dto?.RpmLedCount is > 0 and <= RpmLedSegment.MaxLeds
            ? dto.RpmLedCount.Value
            : 10;

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
                RpmLedCount = totalLedCount,
                RpmSegments = ReadSegments(dto.RpmSegments, totalLedCount),
            }
        };
    }

    public string ToJson(IDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        var serialDevice = device as SerialDevice;
        var totalLedCount = Math.Clamp(serialDevice?.RpmLedCount ?? 10, 1, RpmLedSegment.MaxLeds);
        var segments = RpmLedSegment.Normalize(serialDevice?.RpmSegments, totalLedCount);

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
            RpmLedCount = totalLedCount,
            RpmSegments = segments.Select(x => new RpmLedSegmentDto
            {
                ThresholdPercent = x.ThresholdPercent,
                Flicker = x.Flicker,
                Leds = x.Leds.Take(totalLedCount).Select(led => (string?)led).ToList(),
            }).ToList(),
        };

        return JsonSerializer.Serialize(dto);
    }

    private static List<RpmLedSegment> ReadSegments(
        List<RpmLedSegmentDto>? dtoSegments,
        int totalLedCount)
    {
        if (dtoSegments is not { Count: > 0 })
        {
            return RpmLedSegment.CreateDefault(totalLedCount);
        }

        if (dtoSegments.All(x => x.Leds is not { Count: > 0 }))
        {
            return RpmLedSegment.Normalize(LegacyFrames(dtoSegments, totalLedCount), totalLedCount);
        }

        var segments = dtoSegments.Select(x => new RpmLedSegment(
            x.Leds ?? [],
            x.ThresholdPercent,
            x.Flicker));

        return RpmLedSegment.Normalize(segments, totalLedCount);
    }

    /// <summary>
    /// Older configurations stored additive segments. Convert them into
    /// cumulative frames so their visual behaviour is preserved.
    /// </summary>
    private static List<RpmLedSegment> LegacyFrames(
        List<RpmLedSegmentDto> dtoSegments,
        int totalLedCount)
    {
        var cumulative = new List<string?>();
        var frames = new List<RpmLedSegment>();

        foreach (var dto in dtoSegments.OrderBy(x => x.ThresholdPercent))
        {
            var count = Math.Clamp(dto.LedCount, 0, totalLedCount - cumulative.Count);
            var color = string.IsNullOrWhiteSpace(dto.Color) ? "G" : dto.Color;
            cumulative.AddRange(Enumerable.Repeat<string?>(color, count));

            frames.Add(new RpmLedSegment(
                Enumerable.Range(0, totalLedCount)
                    .Select(i => i < cumulative.Count ? cumulative[i] : RpmLedSegment.OffLed),
                dto.ThresholdPercent));
        }

        return frames;
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
        public List<RpmLedSegmentDto>? RpmSegments { get; init; }
    }

    private class RpmLedSegmentDto
    {
        public int ThresholdPercent { get; init; }
        public bool Flicker { get; init; }
        public List<string?>? Leds { get; init; }

        // Legacy fields, used only when migrating older configurations.
        public int LedCount { get; init; }
        public string? Color { get; init; }
    }
}
