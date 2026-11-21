using System.Text.Json;
using OpenGrid.Models;
using Serilog;

namespace OpenGrid.Services.Sound;

public sealed class BassShakerPresetService : IBassShakerPresetService
{
    private static readonly string PresetsDirectory = Path.Combine(
        Program.AppDataDirectory,
        "bass-shaker-presets");

    private readonly List<BassShakerPreset> _presets = [];

    public event EventHandler<BassShakerPreset>? PresetAdded;
    public event EventHandler<BassShakerPreset>? PresetUpdated;
    public event EventHandler<BassShakerPreset>? PresetRemoved;

    public BassShakerPresetService()
    {
        Directory.CreateDirectory(PresetsDirectory);
        LoadAll();
    }

    public IReadOnlyList<BassShakerPreset> GetAll() => _presets;

    public BassShakerPreset? GetById(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return _presets.FirstOrDefault(preset => preset.Id == id);
    }

    public BassShakerPreset Create(string name, BassShakerConfiguration? configuration = null)
    {
        var preset = new BassShakerPreset
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = string.IsNullOrWhiteSpace(name) ? "New Preset" : name.Trim(),
            Configuration = configuration?.Clone() ?? new BassShakerConfiguration(),
        };

        _presets.Add(preset);
        SaveToFile(preset);
        PresetAdded?.Invoke(this, preset);
        return preset;
    }

    public void Save(BassShakerPreset preset)
    {
        if (string.IsNullOrWhiteSpace(preset.Id))
        {
            preset.Id = Guid.NewGuid().ToString("N");
        }

        if (!_presets.Contains(preset))
        {
            _presets.Add(preset);
        }

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

    private void LoadAll()
    {
        foreach (var file in Directory.GetFiles(PresetsDirectory, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var preset = JsonSerializer.Deserialize<BassShakerPreset>(json);
                if (preset is null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(preset.Id))
                {
                    preset.Id = Path.GetFileNameWithoutExtension(file);
                }

                if (_presets.Any(x => x.Id == preset.Id))
                {
                    Log.Warning("Skipping duplicate bass shaker preset id {PresetId} from {File}", preset.Id, file);
                    continue;
                }

                _presets.Add(preset);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load bass shaker preset from {File}", file);
            }
        }
    }

    private static void SaveToFile(BassShakerPreset preset)
    {
        try
        {
            File.WriteAllText(GetPresetPath(preset), JsonSerializer.Serialize(preset));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save bass shaker preset {PresetId}", preset.Id);
        }
    }

    private static void DeleteFile(BassShakerPreset preset)
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
            Log.Error(ex, "Failed to delete bass shaker preset {PresetId}", preset.Id);
        }
    }

    private static string GetPresetPath(BassShakerPreset preset) =>
        Path.Combine(PresetsDirectory, $"{preset.Id}.json");
}
