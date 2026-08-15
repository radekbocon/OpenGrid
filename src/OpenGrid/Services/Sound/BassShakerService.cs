using OpenGrid.Models;
using OpenGrid.Services.Devices;
using OpenGrid.Services.Telemetry;
using PortAudioSharp;
using Serilog;

namespace OpenGrid.Services.Sound;

public interface IBassShakerService : IDisposable
{
    void ConfigureDevice(SoundDevice device);
    void RemoveDevice(string deviceId);
}

public sealed class BassShakerService : IBassShakerService, IDisposable
{
    private readonly ITelemetryService _telemetryService;
    private readonly IDeviceService _deviceService;
    private readonly Dictionary<string, BassShakerOutput> _outputs = [];
    private readonly object _sync = new();
    private bool _portAudioReady;
    private bool _disposed;

    public BassShakerService(ITelemetryService telemetryService, IDeviceService deviceService)
    {
        _telemetryService = telemetryService;
        _deviceService = deviceService;
        _telemetryService.TelemetryReceived += OnTelemetryReceived;

        lock (_sync)
        {
            foreach (var device in _deviceService.GetPersistedDevices())
            {
                if (device is SoundDevice soundDevice)
                {
                    ConfigureDeviceInternal(soundDevice);
                }
            }
        }
    }

    public void ConfigureDevice(SoundDevice device)
    {
        lock (_sync)
        {
            ConfigureDeviceInternal(device);
        }
    }

    public void RemoveDevice(string deviceId)
    {
        lock (_sync)
        {
            if (_outputs.Remove(deviceId, out var output))
            {
                output.Dispose();
            }
        }
    }

    private void ConfigureDeviceInternal(SoundDevice device)
    {
        var bassShaker = device.BassShaker;

        if (!device.IsEnabled || bassShaker is not { IsEnabled: true } || bassShaker.Inputs.Count == 0)
        {
            RemoveOutput(device.Id);
            return;
        }

        var enabledInputs = bassShaker.Inputs.Where(x => x.IsEnabled).ToList();
        if (enabledInputs.Count == 0)
        {
            RemoveOutput(device.Id);
            return;
        }

        if (!int.TryParse(device.Id, out var deviceIndex))
        {
            RemoveOutput(device.Id);
            return;
        }

        if (_outputs.TryGetValue(device.Id, out var currentOutput))
        {
            if (!currentOutput.HasInputs(bassShaker.Volume, enabledInputs))
            {
                currentOutput.Reconfigure(bassShaker.Volume, enabledInputs);
            }
            return;
        }

        try
        {
            if (!_portAudioReady)
            {
                PortAudio.Initialize();
                _portAudioReady = true;
            }

            var output = new BassShakerOutput(deviceIndex, bassShaker.Volume, enabledInputs);
            _outputs[device.Id] = output;
            Log.Information("Started bass shaker output on device {Device}", device.Name);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to start bass shaker output on device {Device}", device.Name);
        }
    }

    private void RemoveOutput(string deviceId)
    {
        if (_outputs.Remove(deviceId, out var output))
        {
            output.Dispose();
        }
    }

    private void OnTelemetryReceived(object? sender, TelemetryEventArgs e)
    {
        var telemetry = e.Telemetry;

        lock (_sync)
        {
            foreach (var output in _outputs.Values)
            {
                output.ApplyTelemetry(telemetry);
            }
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

        lock (_sync)
        {
            foreach (var output in _outputs.Values)
            {
                output.Dispose();
            }
            _outputs.Clear();
        }
    }
}
