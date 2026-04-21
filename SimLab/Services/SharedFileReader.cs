using System;
using System.Threading;
using System.Threading.Tasks;
using SimLab.Models;

namespace SimLab.Services;

/// <summary>
/// Service for reading telemetry data from shared files written by the bridge
/// Polls the shared memory files continuously for new data
/// </summary>
public class SharedFileReader : IDisposable
{
    private const int PollIntervalMs = 16; // ~60 Hz

    private readonly ISharedMemoryReader _memoryReader = new AccSharedMemoryReader();
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _readingTask;
    private bool _disposed;

    public event EventHandler<TelemetryDataEventArgs>? TelemetryDataReceived;
    public event EventHandler<string>? ConnectionStatusChanged;

    public bool IsConnected { get; private set; }

    public void StartReading()
    {
        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _readingTask = ReadPollingLoopAsync(_cancellationTokenSource.Token);

            IsConnected = true;
            OnConnectionStatusChanged("Connected to shared files");
        }
        catch (Exception ex)
        {
            OnConnectionStatusChanged($"Error starting file reader: {ex.Message}");
        }
    }

    private async Task ReadPollingLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var snapshot = _memoryReader.ReadTelemetryData();
                if (snapshot != null)
                {
                    TelemetryDataReceived?.Invoke(this, new TelemetryDataEventArgs { Snapshot = snapshot });
                }

                await Task.Delay(PollIntervalMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                OnConnectionStatusChanged($"Error reading shared files: {ex.Message}");
                await Task.Delay(1000, cancellationToken); // Wait longer on error
            }
        }
    }

    public void StopReading()
    {
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            _readingTask?.Wait(5000); // Wait max 5 seconds
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        _readingTask = null;
        IsConnected = false;
        OnConnectionStatusChanged("Disconnected");
    }

    protected virtual void OnConnectionStatusChanged(string message)
    {
        ConnectionStatusChanged?.Invoke(this, message);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        StopReading();
        _disposed = true;
    }
}

public class TelemetryDataEventArgs : EventArgs
{
    public TelemetrySnapshot? Snapshot { get; set; }
}
