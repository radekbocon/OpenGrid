using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using SimLab.Models;

namespace SimLab.Services.Devices.Moza;

public class MozaWheelDevice : ITelemetryDevice
{
    private const int BaudRate = 115200;
    private const int UpdateIntervalMs = 33;

    private readonly SerialPort _serialPort;
    private readonly object _sendLock = new();
    private CancellationTokenSource? _updateLoopCts;
    private Task? _updateLoopTask;
    private ushort _lastRpm;
    private ushort _lastMaxRpm;
    private bool _disposed;

    public MozaWheelDevice(string portName)
    {
        Id = $"moza_{Path.GetFileName(portName)}";
        Name = $"Moza Wheel ({portName})";
        _serialPort = new SerialPort(portName, BaudRate)
        {
            ReadTimeout = 1000,
            WriteTimeout = 1000,
        };
    }

    public string Id { get; }
    public string Name { get; }
    public DeviceStatus Status { get; private set; } = DeviceStatus.Disconnected;

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (Status == DeviceStatus.Connected)
        {
            return true;
        }

        Status = DeviceStatus.Connecting;

        try
        {
            await Task.Run(() =>
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                }
            }, cancellationToken);

            Status = DeviceStatus.Connected;
            Log.Information("Moza wheel connected: {PortName}", _serialPort.PortName);

            _updateLoopCts = new CancellationTokenSource();
            _updateLoopTask = UpdateLoopAsync(_updateLoopCts.Token);

            return true;
        }
        catch (Exception ex)
        {
            Status = DeviceStatus.Error;
            Log.Error(ex, "Failed to connect to Moza wheel: {Message}", ex.Message);
            return false;
        }
    }

    public void Disconnect()
    {
        _updateLoopCts?.Cancel();

        if (_serialPort.IsOpen)
        {
            _serialPort.Close();
        }

        Status = DeviceStatus.Disconnected;
        Log.Information("Moza wheel disconnected: {PortName}", _serialPort.PortName);
    }

    public void ProcessTelemetry(TelemetryRecord telemetry)
    {
        _lastRpm = (ushort)telemetry.EngineRpm;
        _lastMaxRpm = (ushort)telemetry.MaxRpm;
    }

    private async Task UpdateLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_serialPort.IsOpen && Status == DeviceStatus.Connected)
                {
                    var frame = MozaProtocol.BuildRpmLedFrame(_lastRpm, _lastMaxRpm);
                    lock (_sendLock)
                    {
                        _serialPort.Write(frame, 0, frame.Length);
                    }
                }

                await Task.Delay(UpdateIntervalMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error sending RPM LED update to Moza wheel");
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Disconnect();
        _serialPort.Dispose();
        _disposed = true;
    }
}
