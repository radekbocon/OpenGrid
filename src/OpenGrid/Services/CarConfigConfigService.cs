using System.Text.Json;
using OpenGrid.Models;
using OpenGrid.Models.Telemetry;
using OpenGrid.Services.Telemetry;
using Serilog;

namespace OpenGrid.Services;

public sealed class CarConfigConfigService : ICarConfigService
{
    private static readonly string CarsDirectory = Path.Combine(Program.AppDataDirectory, "cars");

    private readonly ITelemetryService _telemetryService;
    private readonly List<CarProfile> _cars = [];
    private readonly HashSet<string> _knownCarKeys = [];

    public event EventHandler<CarProfile>? CarAdded;
    public event EventHandler<CarProfile>? CarRemoved;
    public event EventHandler<CarProfile>? CarUpdated;

    public CarConfigConfigService(ITelemetryService telemetryService)
    {
        _telemetryService = telemetryService;
        Directory.CreateDirectory(CarsDirectory);
        LoadAll();
        _telemetryService.TelemetryReceived += OnTelemetryReceived;
    }

    public IReadOnlyList<CarProfile> GetAll() => _cars;

    public CarProfile? GetByCarName(string carName)
    {
        return _cars.FirstOrDefault(c => c.Name == carName);
    }

    public void Save(CarProfile car)
    {
        SaveToFile(car);
        CarUpdated?.Invoke(this, car);
    }

    public void Remove(string carKey)
    {
        var car = _cars.FirstOrDefault(c => c.Name == carKey);
        if (car is null) return;

        _cars.Remove(car);
        _knownCarKeys.Remove(car.Name);
        DeleteFile(car);
        CarRemoved?.Invoke(this, car);
    }

    private void OnTelemetryReceived(object? sender, TelemetryEventArgs e)
    {
        var car = e.Telemetry.Car;
        if (string.IsNullOrWhiteSpace(car.Name) || car == Car.Unknown)
            return;

        if (_knownCarKeys.Contains(car.Name))
            return;

        var existing = GetByCarName(car.Name);
        if (existing is not null)
        {
            _knownCarKeys.Add(car.Name);
            return;
        }

        var maxRpm = (int)e.Telemetry.MaxRpm;
        if (maxRpm <= 0) maxRpm = 8000;

        var profile = new CarProfile
        {
            Name = car.Name,
            MaxRpm = maxRpm,
            RedlineRpm = (int)(maxRpm * 0.95),
        };

        _cars.Add(profile);
        _knownCarKeys.Add(car.Name);
        SaveToFile(profile);
        CarAdded?.Invoke(this, profile);
    }

    private void LoadAll()
    {
        if (!Directory.Exists(CarsDirectory))
            return;

        foreach (var file in Directory.GetFiles(CarsDirectory, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var car = JsonSerializer.Deserialize<CarProfile>(json);
                if (car is not null)
                {
                    _cars.Add(car);
                    if (!string.IsNullOrEmpty(car.Name))
                    {
                        _knownCarKeys.Add(car.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load car from {File}", file);
            }
        }
    }

    private static void SaveToFile(CarProfile car)
    {
        try
        {
            var path = GetCarPath(car);
            File.WriteAllText(path, JsonSerializer.Serialize(car));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save car {CarKey}", car.Name);
        }
    }

    private static void DeleteFile(CarProfile car)
    {
        try
        {
            var path = GetCarPath(car);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete car file {CarKey}", car.Name);
        }
    }

    private static string GetCarPath(CarProfile car) => Path.Combine(CarsDirectory, $"{car.Name}.json");
}
