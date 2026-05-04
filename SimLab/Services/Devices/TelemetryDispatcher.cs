using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using SimLab.Models;

namespace SimLab.Services.Devices;

public interface ITelemetryDispatcher
{
    void RegisterDevice(ITelemetryDevice device);
    void UnregisterDevice(string deviceId);
    IReadOnlyCollection<ITelemetryDevice> Devices { get; }
    void Dispatch(TelemetryRecord telemetry);
}

public class TelemetryDispatcher : ITelemetryDispatcher
{
    private readonly ConcurrentDictionary<string, ITelemetryDevice> _devices = new();

    public IReadOnlyCollection<ITelemetryDevice> Devices => _devices.Values.ToList().AsReadOnly();

    public void RegisterDevice(ITelemetryDevice device)
    {
        if (_devices.TryAdd(device.Id, device))
        {
            Log.Information("Registered device: {DeviceName} ({DeviceId})", device.Name, device.Id);
        }
    }

    public void UnregisterDevice(string deviceId)
    {
        if (_devices.TryRemove(deviceId, out var device))
        {
            device.Dispose();
            Log.Information("Unregistered device: {DeviceName} ({DeviceId})", device.Name, deviceId);
        }
    }

    public void Dispatch(TelemetryRecord telemetry)
    {
        foreach (var device in _devices.Values)
        {
            if (device.Status == DeviceStatus.Connected)
            {
                try
                {
                    device.ProcessTelemetry(telemetry);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error processing telemetry on device {DeviceName}", device.Name);
                }
            }
        }
    }
}
