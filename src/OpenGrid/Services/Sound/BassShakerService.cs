using OpenGrid.Models;
using OpenGrid.Models.Telemetry;
using OpenGrid.Services.Devices;
using OpenGrid.Services.Telemetry;
using Serilog;

namespace OpenGrid.Services.Sound;

public interface IBassShakerService : IDisposable
{
    Task InitializeAsync();
    void ConfigureDevice(SoundDevice device);
    void RemoveDevice(string deviceId);
}

public sealed class BassShakerService : IBassShakerService
{
    private readonly ITelemetryService _telemetryService;
    private readonly IDeviceService _deviceService;
    private readonly IBassShakerPresetService _presetService;
    private readonly Dictionary<string, SoundDevice> _devices = [];
    private readonly Dictionary<string, BassShakerOutputState> _outputs = [];
    private readonly SemaphoreSlim _sync = new(1, 1);
    private SteamGame? _currentGame;
    private Car? _currentCar;
    private bool _disposed;

    public BassShakerService(
        ITelemetryService telemetryService,
        IDeviceService deviceService,
        IBassShakerPresetService presetService)
    {
        _telemetryService = telemetryService;
        _deviceService = deviceService;
        _presetService = presetService;
        _telemetryService.TelemetryReceived += OnTelemetryReceived;
        _telemetryService.TelemetryStatusChanged += OnTelemetryStatusChanged;
        _presetService.PresetUpdated += OnPresetChanged;
        _presetService.PresetRemoved += OnPresetRemoved;
    }

    public async Task InitializeAsync()
    {
        try
        {
            await _sync.WaitAsync();
            await Task.Run(() =>
            {
                foreach (var device in _deviceService.GetPersistedDevices().OfType<SoundDevice>())
                {
                    MigrateLegacyConfiguration(device);
                    _devices[device.Id] = device;
                    ConfigureDeviceInternal(device, force: true);
                }
            });
        }
        finally
        {
            _sync.Release();
        }
    }

    public void ConfigureDevice(SoundDevice device)
    {
        try
        {
            _sync.Wait();
            _devices[device.Id] = device;
            ConfigureDeviceInternal(device, force: false);
        }
        finally
        {
            _sync.Release();
        }
    }

    public void RemoveDevice(string deviceId)
    {
        try
        {
            _sync.Wait();
            _devices.Remove(deviceId);
            if (_outputs.Remove(deviceId, out var output))
            {
                output.Output.Dispose();
            }
        }
        finally
        {
            _sync.Release();
        }
    }

    private void MigrateLegacyConfiguration(SoundDevice device)
    {
        if (device.BassShaker is not { } legacy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(device.DefaultBassShakerPresetId) &&
            legacy.Inputs.Count > 0)
        {
            var preset = _presetService.Create($"{device.Name} (Imported)", legacy);
            device.DefaultBassShakerPresetId = preset.Id;
            Log.Information(
                "Migrated bass shaker configuration for {Device} to preset {Preset}",
                device.Name,
                preset.Name);
        }

        device.BassShaker = null;
        _deviceService.SaveDevice(device);
    }

    private void ConfigureDeviceInternal(SoundDevice device, bool force)
    {
        var preset = ResolvePreset(device);
        var configuration = preset?.Configuration;

        if (!device.IsEnabled || configuration is null)
        {
            RemoveOutput(device.Id);
            return;
        }

        var enabledInputs = configuration.Inputs
            .Where(static x => x is { IsEnabled: true, Mappings.Count: > 0 })
            .ToList();
        if (enabledInputs.Count == 0)
        {
            RemoveOutput(device.Id);
            return;
        }

        if (_outputs.TryGetValue(device.Id, out var state))
        {
            if (!force && state.PresetId == preset!.Id)
            {
                return;
            }

            state.Output.Reconfigure(configuration.Volume, enabledInputs);
            state.PresetId = preset!.Id;
            return;
        }

        try
        {
            SdlAudio.EnsureInitialized();

            var output = new BassShakerOutput(device.Id, configuration.Volume, enabledInputs);
            _outputs[device.Id] = new BassShakerOutputState(output, preset!.Id);
            Log.Information(
                "Started bass shaker output on device {Device} using preset {Preset}",
                device.Name,
                preset.Name);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to start bass shaker output on device {Device}", device.Name);
        }
    }

    private BassShakerPreset? ResolvePreset(SoundDevice device)
    {
        var gameAppId = _currentGame?.AppId;

        var matchingOverride = device.BassShakerOverrides
            .Where(overrideSettings => Matches(overrideSettings, gameAppId, _currentCar))
            .OrderByDescending(static overrideSettings => GetSpecificity(overrideSettings))
            .Select(overrideSettings => _presetService.GetById(overrideSettings.PresetId))
            .FirstOrDefault(static preset => preset is not null);

        return matchingOverride ?? _presetService.GetById(device.DefaultBassShakerPresetId);
    }

    private static bool Matches(BassShakerPresetOverride overrideSettings, int? gameAppId, Car? car)
    {
        var hasGame = overrideSettings.GameAppId is not null;
        var hasCar = overrideSettings.Car is not null;

        if (!hasGame && !hasCar)
        {
            return false;
        }

        if (overrideSettings.GameAppId is { } requiredGameAppId &&
            (!gameAppId.HasValue || requiredGameAppId != gameAppId.Value))
        {
            return false;
        }

        return !hasCar ||
               (car is not null &&
                string.Equals(overrideSettings.Car?.Name, car.Value.Name, StringComparison.OrdinalIgnoreCase));
    }

    private static int GetSpecificity(BassShakerPresetOverride overrideSettings)
    {
        var score = overrideSettings.GameAppId is not null ? 2 : 0;
        if (overrideSettings.Car is not null)
        {
            // A game + car match is more specific than a game-only match, which in turn
            // is more specific than a car-only match.
            score++;
        }

        return score;
    }

    private void OnPresetChanged(object? sender, BassShakerPreset preset)
    {
        try
        {
            _sync.Wait();
            foreach (var device in _devices.Values)
            {
                if (device.DefaultBassShakerPresetId == preset.Id ||
                    device.BassShakerOverrides.Any(x => x.PresetId == preset.Id))
                {
                    ConfigureDeviceInternal(device, force: true);
                }
            }
        }
        finally
        {
            _sync.Release();
        }
    }

    private void OnPresetRemoved(object? sender, BassShakerPreset preset)
    {
        try
        {
            _sync.Wait();
            foreach (var device in _devices.Values)
            {
                var deviceChanged = false;

                if (device.DefaultBassShakerPresetId == preset.Id)
                {
                    device.DefaultBassShakerPresetId = null;
                    deviceChanged = true;
                }

                if (device.BassShakerOverrides.RemoveAll(x => x.PresetId == preset.Id) > 0)
                {
                    deviceChanged = true;
                }

                if (!deviceChanged)
                {
                    continue;
                }

                _deviceService.SaveDevice(device);
                ConfigureDeviceInternal(device, force: true);
            }
        }
        finally
        {
            _sync.Release();
        }
    }

    private void RemoveOutput(string deviceId)
    {
        if (_outputs.Remove(deviceId, out var output))
        {
            output.Output.Dispose();
        }
    }

    private void OnTelemetryReceived(object? sender, TelemetryEventArgs e)
    {
        var telemetry = e.Telemetry;

        try
        {
            _sync.Wait();
            var gameChanged = _currentGame?.AppId != e.Game.AppId;
            var carChanged = _currentCar != e.Telemetry.Car;

            if (gameChanged || carChanged)
            {
                _currentGame = e.Game;
                _currentCar = e.Telemetry.Car;

                foreach (var device in _devices.Values)
                {
                    ConfigureDeviceInternal(device, force: false);
                }
            }

            foreach (var output in _outputs.Values)
            {
                output.Output.ApplyTelemetry(telemetry);
            }
        }
        finally
        {
            _sync.Release();
        }
    }

    private void OnTelemetryStatusChanged(object? sender, TelemetryConnectionStatus status)
    {
        if (status == TelemetryConnectionStatus.Connected)
        {
            return;
        }

        try
        {
            _sync.Wait();
            _currentGame = null;
            _currentCar = null;

            foreach (var output in _outputs.Values)
            {
                output.Output.ClearTelemetry();
            }

            // Fall back to the device default preset now that there is no game or car context.
            foreach (var device in _devices.Values)
            {
                ConfigureDeviceInternal(device, force: false);
            }
        }
        finally
        {
            _sync.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        _telemetryService.TelemetryReceived -= OnTelemetryReceived;
        _telemetryService.TelemetryStatusChanged -= OnTelemetryStatusChanged;
        _presetService.PresetUpdated -= OnPresetChanged;
        _presetService.PresetRemoved -= OnPresetRemoved;

        try
        {
            _sync.Wait();
            foreach (var output in _outputs.Values)
            {
                output.Output.Dispose();
            }
            _outputs.Clear();
            _devices.Clear();
        }
        finally
        {
            _sync.Release();
        }
    }

    private sealed class BassShakerOutputState
    {
        public BassShakerOutputState(BassShakerOutput output, string presetId)
        {
            Output = output;
            PresetId = presetId;
        }

        public BassShakerOutput Output { get; }
        public string PresetId { get; set; }
    }
}
