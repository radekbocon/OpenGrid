using System;
using System.IO;
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
    private const string ShmPhysicsPath = "/dev/shm/simlab_physics";
    private const int PollIntervalMs = 16; // ~60 Hz

    private readonly SharedMemoryReader _memoryReader = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _readingTask;
    private bool _disposed = false;
    private TelemetrySnapshot? _lastSnapshot;

    public event EventHandler<TelemetryDataEventArgs>? TelemetryDataReceived;
    public event EventHandler<string>? ConnectionStatusChanged;

    public bool IsConnected { get; private set; }

    public void StartReading()
    {
        try
        {
            // Ensure the directory exists
            var dir = Path.GetDirectoryName(ShmPhysicsPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir!);
            }

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
                    _lastSnapshot = snapshot;
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

    private bool SnapshotEqual(TelemetrySnapshot? current, TelemetrySnapshot? last)
    {
        if (current == null || last == null)
            return false;

        // Compare key telemetry values to detect actual updates
        return current.CurrentLap == last.CurrentLap &&
               Math.Abs(current.SpeedKmh - last.SpeedKmh) < 0.1f &&
               Math.Abs(current.EngineRpm - last.EngineRpm) < 1f &&
               Math.Abs(current.Gas - last.Gas) < 0.001f &&
               Math.Abs(current.Brake - last.Brake) < 0.001f;
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
        _lastSnapshot = null;
        IsConnected = false;
        OnConnectionStatusChanged("Disconnected");
    }

    protected virtual void OnConnectionStatusChanged(string message)
    {
        ConnectionStatusChanged?.Invoke(this, message);
    }

    public void Dispose()
    {
        if (_disposed) return;
        StopReading();
        _disposed = true;
    }
}

/// <summary>
/// Event args for telemetry data received
/// </summary>
public class TelemetryDataEventArgs : EventArgs
{
    public TelemetrySnapshot? Snapshot { get; set; }
}
