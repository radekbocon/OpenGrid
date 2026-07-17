using System.Text.Json;
using OpenGrid.Models;
using OpenGrid.Services.Telemetry;

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

    public CarProfile? GetById(string id)
    {
        return _cars.FirstOrDefault(c => c.Id == id);
    }

    public CarProfile? GetByCarKey(string carKey)
    {
        return _cars.FirstOrDefault(c => c.CarKey == carKey);
    }

    public void Save(CarProfile car)
    {
        SaveToFile(car);
        CarUpdated?.Invoke(this, car);
    }

    public void Remove(string id)
    {
        var car = _cars.FirstOrDefault(c => c.Id == id);
        if (car is null) return;

        _cars.Remove(car);
        _knownCarKeys.Remove(car.CarKey);
        DeleteFile(car);
        CarRemoved?.Invoke(this, car);
    }

    private void OnTelemetryReceived(object? sender, TelemetryEventArgs e)
    {
        var carKey = e.Telemetry.Car.Key;
        if (string.IsNullOrWhiteSpace(carKey) || carKey == "unknown_car")
            return;

        if (_knownCarKeys.Contains(carKey))
            return;

        var existing = GetByCarKey(carKey);
        if (existing is not null)
        {
            _knownCarKeys.Add(carKey);
            return;
        }

        var maxRpm = (int)e.Telemetry.MaxRpm;
        if (maxRpm <= 0) maxRpm = 8000;

        var profile = new CarProfile
        {
            CarKey = carKey,
            Name = e.Telemetry.Car.DisplayName,
            MaxRpm = maxRpm,
            RedlineRpm = (int)(maxRpm * 0.95)
        };

        _cars.Add(profile);
        _knownCarKeys.Add(carKey);
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
                    if (!string.IsNullOrEmpty(car.CarKey))
                    {
                        _knownCarKeys.Add(car.CarKey);
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Failed to load car from {File}", file);
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
            Serilog.Log.Warning(ex, "Failed to save car {CarId}", car.Id);
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
            Serilog.Log.Warning(ex, "Failed to delete car file {CarId}", car.Id);
        }
    }

    private static string GetCarPath(CarProfile car) => Path.Combine(CarsDirectory, $"{car.Id}.json");
}
