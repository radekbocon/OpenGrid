using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Media;
using OpenGrid.Models;
using Serilog;

namespace OpenGrid.Services.Devices;

public sealed class LedPresetService : ILedPresetService
{
    private const int MinLeds = 1;

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
            LedCount = Math.Max(MinLeds, ledCount),
        };
    }

    private static void NormalizeConfiguration(LedPresetConfiguration configuration)
    {
        configuration.LedCount = Math.Max(MinLeds, configuration.LedCount);
        configuration.Effects.RemoveAll(static effect =>
            effect is not { } settings || !Enum.IsDefined(settings.Type));

        foreach (var effect in configuration.Effects)
        {
            if (effect.Type == LedEffectType.Rpm && effect.RpmSegments.Count == 0)
            {
                effect.RpmSegments = RpmLedSegment.CreateDefault(configuration.LedCount);
            }
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
        var dtoConfiguration = dto.Configuration;
        if (dtoConfiguration is null)
        {
            return new LedPreset
            {
                Id = dto.Id,
                Name = dto.Name,
                Configuration = CreateDefaultConfiguration(),
            };
        }

        var ledCount = dtoConfiguration.LedCount is > 0
            ? dtoConfiguration.LedCount.Value
            : 10;

        var effects = (dtoConfiguration.Effects ?? [])
            .Where(static effect => effect is not null)
            .Select(static effect => new LedEffectSettings
            {
                Type = effect.Type,
                IsEnabled = effect.IsEnabled,
                RpmSegments = ParseSegments(effect.RpmSegments),
            })
            .ToList();

        // Migrate presets saved before a preset could hold multiple effects.
        if (dtoConfiguration.Effects is null)
        {
            effects.Add(new LedEffectSettings
            {
                Type = dtoConfiguration.EffectType ?? LedEffectType.Rpm,
                IsEnabled = true,
                RpmSegments = ParseSegments(dtoConfiguration.RpmSegments),
            });
        }

        return new LedPreset
        {
            Id = dto.Id,
            Name = dto.Name,
            Configuration = new LedPresetConfiguration
            {
                LedCount = ledCount,
                Effects = effects,
            },
        };
    }

    private static List<RpmLedSegment> ParseSegments(IEnumerable<RpmLedSegmentDto>? segments) =>
        (segments ?? [])
            .Select(static segment => new RpmLedSegment(
                (segment.Leds ?? []).Select(static led => Color.Parse(led)),
                segment.ThresholdPercent,
                segment.Flicker))
            .ToList();

    private static LedPresetDto ToDto(LedPreset preset)
    {
        return new LedPresetDto
        {
            Id = preset.Id,
            Name = preset.Name,
            Configuration = new LedPresetConfigurationDto
            {
                LedCount = preset.Configuration.LedCount,
                Effects = preset.Configuration.Effects
                    .Where(static effect => effect is not null)
                    .Select(static effect => new LedEffectSettingsDto
                    {
                        Type = effect.Type,
                        IsEnabled = effect.IsEnabled,
                        RpmSegments = effect.RpmSegments
                            .Select(static segment => new RpmLedSegmentDto
                            {
                                ThresholdPercent = segment.ThresholdPercent,
                                Flicker = segment.Flicker,
                                Leds = segment.Leds.Select(static led => led.ToString()).ToList(),
                            })
                            .ToList(),
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
        public int? LedCount { get; init; }
        public List<LedEffectSettingsDto>? Effects { get; init; }

        // Legacy fields, read only. Presets written before effects were
        // introduced stored a single effect directly on the configuration.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public LedEffectType? EffectType { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<RpmLedSegmentDto>? RpmSegments { get; init; }
    }

    private class LedEffectSettingsDto
    {
        public LedEffectType Type { get; init; }
        public bool IsEnabled { get; init; } = true;
        public List<RpmLedSegmentDto>? RpmSegments { get; init; }
    }

    private class RpmLedSegmentDto
    {
        public int ThresholdPercent { get; init; }
        public bool Flicker { get; init; }
        public List<string>? Leds { get; init; }
    }
}
