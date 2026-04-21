using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SimLab.Models;

namespace SimLab.Services;

/// <summary>
/// Service for reading telemetry data from shared files written by the bridge
/// </summary>
public class SharedFileReader : IDisposable
{
    private const string SHM_PHYSICS_PATH = "/dev/shm/simlab_physics";

    private FileSystemWatcher? _watcher;
    private bool _disposed = false;

    public event EventHandler<TelemetryDataEventArgs>? TelemetryDataReceived;
    public event EventHandler<string>? ConnectionStatusChanged;

    public bool IsConnected { get; private set; }

    public void StartReading()
    {
        try
        {
            // Ensure the directory exists
            var dir = Path.GetDirectoryName(SHM_PHYSICS_PATH);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir!);
            }

            _watcher = new FileSystemWatcher(dir!)
            {
                Filter = Path.GetFileName(SHM_PHYSICS_PATH),
                NotifyFilter = NotifyFilters.LastWrite
            };

            _watcher.Changed += OnFileChanged;
            _watcher.EnableRaisingEvents = true;

            IsConnected = true;
            OnConnectionStatusChanged("Connected to shared files");
        }
        catch (Exception ex)
        {
            OnConnectionStatusChanged($"Error starting file reader: {ex.Message}");
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            if (File.Exists(SHM_PHYSICS_PATH))
            {
                var json = File.ReadAllText(SHM_PHYSICS_PATH);
                var snapshot = System.Text.Json.JsonSerializer.Deserialize<TelemetrySnapshot>(json);
                if (snapshot != null)
                {
                    TelemetryDataReceived?.Invoke(this, new TelemetryDataEventArgs { Snapshot = snapshot });
                }
            }
        }
        catch (Exception ex)
        {
            OnConnectionStatusChanged($"Error reading shared file: {ex.Message}");
        }
    }

    public void StopReading()
    {
        _watcher?.Dispose();
        _watcher = null;
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
