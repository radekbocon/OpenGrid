using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using SimLab.Models;

namespace SimLab.Services.Telemetry;

/// <summary>
/// Service for reading telemetry data from shared files written by the bridge
/// Polls the shared memory files continuously for new data
/// </summary>
public class TelemetryService : ITelemetryService
{
    private const int PollIntervalMs = 1000 / 30;

    private readonly List<ITelemetryClient> _telemetryClients;
    private readonly SharedMemoryBridgeLauncher _sharedMemoryBridgeLauncher;
    private readonly SteamWatcher _steamWatcher;

    private ITelemetryClient? _telemetryClient;

    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed;

    public event EventHandler<TelemetryEventArgs>? TelemetryReceived;
    public event EventHandler<TelemetryConnectionStatus>? TelemetryStatusChanged;

    public TelemetryConnectionStatus ConnectionStatus { get; private set; }
    public SteamGame? CurrentGame { get; private set; }

    public TelemetryService(IEnumerable<ITelemetryClient> telemetryClients,
        SharedMemoryBridgeLauncher sharedMemoryBridgeLauncher, SteamWatcher steamWatcher)
    {
        var clients = telemetryClients.ToList();
        _telemetryClients = clients.ToList();
        _sharedMemoryBridgeLauncher = sharedMemoryBridgeLauncher;
        _steamWatcher = steamWatcher;

        _steamWatcher.GameStopped += SteamWatcherOnGameStopped;
    }

    public async Task<bool> ConnectAsync(SteamGame game, CancellationToken cancellationToken)
    {
        CurrentGame = game;
        SetConnectionStatus(TelemetryConnectionStatus.Connecting);

        _telemetryClient = _telemetryClients.First(x => x.GetType() == game.TelemetryClientType);

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
            _ = ReadPollingLoopAsync(_cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error starting reading telemetry: {0}", ex.Message);
            TelemetryStatusChanged?.Invoke(this, TelemetryConnectionStatus.Disconnected);
        }
    }

    public void StopReading()
    {
        TelemetryStatusChanged?.Invoke(this, TelemetryConnectionStatus.Disconnected);
        _sharedMemoryBridgeLauncher.StopBridge();
        _telemetryClient?.Stop();
        _telemetryClient = null;
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
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

    private void SteamWatcherOnGameStopped(SteamGameProcess gameProcess)
    {
        if (CurrentGame?.AppId == gameProcess.SteamGame.AppId)
        {
            Dispose();
        }
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

public enum TelemetryConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
}