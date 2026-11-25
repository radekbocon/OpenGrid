using System.Text.Json;
using Avalonia.Media;
using OpenGrid.Models;
using Serilog;

namespace OpenGrid.Services.Devices;

public sealed class LedPresetService : ILedPresetService
{
    private static readonly string PresetsDirectory = Path.Combine(
        Program.AppDataDirectory,
        "led-presets");

    private readonly List<LedPreset> _presets = [];

    public event EventHandler<LedPreset>? PresetAdded;
    public event EventHandler<LedPreset>? PresetUpdated;
    public event EventHandler<LedPreset>? PresetRemoved;

    public LedPresetService()
    {
        Directory.CreateDirectory(PresetsDirectory);
        LoadAll();
    }

    public IReadOnlyList<LedPreset> GetAll() => _presets;

    public LedPreset? GetById(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return _presets.FirstOrDefault(preset => preset.Id == id);
    }

    public LedPreset Create(string name, LedPresetConfiguration? configuration = null)
    {
        var preset = new LedPreset
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = string.IsNullOrWhiteSpace(name) ? "New Preset" : name.Trim(),
            Configuration = configuration?.Clone() ?? CreateDefaultConfiguration(),
        };

        _presets.Add(preset);
        SaveToFile(preset);
        PresetAdded?.Invoke(this, preset);
        return preset;
    }

    public void Save(LedPreset preset)
    {
        if (string.IsNullOrWhiteSpace(preset.Id))
        {
            preset.Id = Guid.NewGuid().ToString("N");
        }

        if (!_presets.Contains(preset))
        {
            _presets.Add(preset);
        }

        NormalizeConfiguration(preset.Configuration);
        SaveToFile(preset);
        PresetUpdated?.Invoke(this, preset);
    }

    public void Remove(string id)
    {
        var preset = GetById(id);
        if (preset is null)
        {
            return;
        }

        _presets.Remove(preset);
        DeleteFile(preset);
        PresetRemoved?.Invoke(this, preset);
    }

    public static LedPresetConfiguration CreateDefaultConfiguration(int ledCount = 10)
    {
        return new LedPresetConfiguration
        {
            EffectType = LedEffectType.Rpm,
            LedCount = Math.Clamp(ledCount, 1, 16),
            RpmSegments = RpmLedSegment.CreateDefault(Math.Clamp(ledCount, 1, 16)),
        };
    }

    private static void NormalizeConfiguration(LedPresetConfiguration configuration)
    {
        if (configuration.RpmSegments.Count == 0)
        {
            configuration.RpmSegments = RpmLedSegment.CreateDefault(configuration.LedCount);
        }
    }

    private void LoadAll()
    {
        foreach (var file in Directory.GetFiles(PresetsDirectory, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var dto = JsonSerializer.Deserialize<LedPresetDto>(json);
                if (dto is null)
                {
                    continue;
                }

                var preset = FromDto(dto);

                if (string.IsNullOrWhiteSpace(preset.Id))
                {
                    preset.Id = Path.GetFileNameWithoutExtension(file);
                }

                if (_presets.Any(x => x.Id == preset.Id))
                {
                    Log.Warning("Skipping duplicate LED preset id {PresetId} from {File}", preset.Id, file);
                    continue;
                }

                NormalizeConfiguration(preset.Configuration);
                _presets.Add(preset);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load LED preset from {File}", file);
            }
        }
    }

    private static LedPreset FromDto(LedPresetDto dto)
    {
        var configuration = dto.Configuration ?? new LedPresetConfigurationDto();
        var ledCount = configuration.LedCount is > 0 ? configuration.LedCount.Value : 10;

        return new LedPreset
        {
            Id = dto.Id,
            Name = dto.Name,
            Configuration = new LedPresetConfiguration
            {
                EffectType = configuration.EffectType,
                LedCount = ledCount,
                RpmSegments = (configuration.RpmSegments ?? [])
                    .Select(x => new RpmLedSegment(
                        (x.Leds ?? []).Select(Color.Parse),
                        x.ThresholdPercent,
                        x.Flicker))
                    .ToList(),
            },
        };
    }

    private static LedPresetDto ToDto(LedPreset preset)
    {
        return new LedPresetDto
        {
            Id = preset.Id,
            Name = preset.Name,
            Configuration = new LedPresetConfigurationDto
            {
                EffectType = preset.Configuration.EffectType,
                LedCount = preset.Configuration.LedCount,
                RpmSegments = preset.Configuration.RpmSegments
                    .Select(x => new RpmLedSegmentDto
                    {
                        ThresholdPercent = x.ThresholdPercent,
                        Flicker = x.Flicker,
                        Leds = x.Leds.Select(led => led.ToString()).ToList(),
                    })
                    .ToList(),
            },
        };
    }

    private static void SaveToFile(LedPreset preset)
    {
        try
        {
            File.WriteAllText(GetPresetPath(preset), JsonSerializer.Serialize(ToDto(preset)));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save LED preset {PresetId}", preset.Id);
        }
    }

    private static void DeleteFile(LedPreset preset)
    {
        try
        {
            var path = GetPresetPath(preset);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete LED preset {PresetId}", preset.Id);
        }
    }

    private static string GetPresetPath(LedPreset preset) =>
        Path.Combine(PresetsDirectory, $"{preset.Id}.json");

    private class LedPresetDto
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public LedPresetConfigurationDto? Configuration { get; init; }
    }

    private class LedPresetConfigurationDto
    {
        public LedEffectType EffectType { get; init; } = LedEffectType.Rpm;
        public int? LedCount { get; init; }
        public List<RpmLedSegmentDto>? RpmSegments { get; init; }
    }

    private class RpmLedSegmentDto
    {
        public int ThresholdPercent { get; init; }
        public bool Flicker { get; init; }
        public List<string>? Leds { get; init; }
    }
}
