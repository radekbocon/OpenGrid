using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using SimLab.Models;
using SimLab.Services.SharedMemory;

namespace SimLab.Services;

public class TelemetryEventArgs : EventArgs
{
    public TelemetryEventArgs(SteamGame game, TelemetryRecord telemetry)
    {
        Game = game;
        Telemetry = telemetry;
    }

    public SteamGame Game { get; }
    public TelemetryRecord Telemetry { get; }
}

public interface ITelemetryService: IDisposable
{
    TelemetryConnectionStatus ConnectionStatus { get; }
    SteamGame? CurrentGame { get; }
    Task<bool> ConnectAsync(SteamGame game, CancellationToken cancellationToken);
    void StartReading();
    void StopReading();
    event EventHandler<TelemetryEventArgs>? TelemetryReceived;
    event EventHandler<TelemetryConnectionStatus>? TelemetryStatusChanged;
}

public enum TelemetryConnectionStatus
{
    None,
    Connecting,
    Connected,
    Disconnected
}

/// <summary>
/// Service for reading telemetry data from shared files written by the bridge
/// Polls the shared memory files continuously for new data
/// </summary>
public class TelemetryService : ITelemetryService
{
    private const int PollIntervalMs = 1000 / 30;

    private readonly AcTelemetryClient _acTelemetryClient;
    private readonly DebugTelemetryClient _debugTelemetryClient;
    private readonly SharedMemoryBridgeLauncher _sharedMemoryBridgeLauncher;
    private readonly Lock _observersLock = new();

    private ITelemetryClient? _telemetryClient;
    
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _readingTask;
    private bool _disposed;

    public event EventHandler<TelemetryEventArgs>? TelemetryReceived;
    public event EventHandler<TelemetryConnectionStatus>? TelemetryStatusChanged; 

    public TelemetryConnectionStatus ConnectionStatus { get; private set; }
    public SteamGame? CurrentGame { get; private set; }

    public TelemetryService(IEnumerable<ITelemetryClient> telemetryClients, SharedMemoryBridgeLauncher sharedMemoryBridgeLauncher)
    {
        var clients = telemetryClients.ToList();
        _acTelemetryClient = (AcTelemetryClient)clients.First(x => x is AcTelemetryClient);
        _debugTelemetryClient = (DebugTelemetryClient)clients.First(x => x is DebugTelemetryClient);
        _sharedMemoryBridgeLauncher = sharedMemoryBridgeLauncher;
    }

    public async Task<bool> ConnectAsync(SteamGame game, CancellationToken cancellationToken)
    {
        SetConnectionStatus(TelemetryConnectionStatus.Connecting);
        
        _telemetryClient = game == SteamGame.Debug ? _debugTelemetryClient : _acTelemetryClient;
        
        CurrentGame = game;
        if (game.RequiresSharedMemoryBridge)
        {
            await _sharedMemoryBridgeLauncher.LaunchBridgeAsync(game, cancellationToken);
        }

        var result = await _telemetryClient.ConnectAsync(cancellationToken);
        SetConnectionStatus(result ? TelemetryConnectionStatus.Connected : TelemetryConnectionStatus.Disconnected);

        return result;
    }

    public void StartReading()
    {
        if (ConnectionStatus != TelemetryConnectionStatus.Connected)
        {
            Log.Warning("Cannot start reading telemetry: not connected");
            return;
        }
        
        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _readingTask = ReadPollingLoopAsync(_cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error starting reading telemetry: {0}", ex.Message);
            TelemetryStatusChanged?.Invoke(this, TelemetryConnectionStatus.Disconnected);
        }
    }
    
    public void StopReading()
    {
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            _sharedMemoryBridgeLauncher.StopBridge();
            _telemetryClient?.Stop();
            _readingTask?.Wait(5000); // Wait max 5 seconds
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
            _telemetryClient = null;
        }

        _readingTask = null;
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

    private async Task ReadPollingLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var snapshot = _telemetryClient?.ReadTelemetry();
                if (snapshot != null && CurrentGame != null)
                {
                    TelemetryReceived?.Invoke(this, new TelemetryEventArgs(CurrentGame, snapshot));
                }
                
                await Task.Delay(PollIntervalMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                TelemetryStatusChanged?.Invoke(this, TelemetryConnectionStatus.Disconnected);
                break;
            }
            catch (Exception)
            {
                TelemetryStatusChanged?.Invoke(this, TelemetryConnectionStatus.Disconnected);
                await Task.Delay(1000, cancellationToken); // Wait longer on error
            }
        }
    }
    
    private void SetConnectionStatus(TelemetryConnectionStatus status)
    {
        ConnectionStatus = status;
        TelemetryStatusChanged?.Invoke(this, status);
    }
}
