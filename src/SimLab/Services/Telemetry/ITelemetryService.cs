using System;
using System.Threading;
using System.Threading.Tasks;
using SimLab.Models;

namespace SimLab.Services.Telemetry;

public interface ITelemetryService : IDisposable
{
    TelemetryConnectionStatus ConnectionStatus { get; }
    SteamGame? CurrentGame { get; }
    Task<bool> ConnectAsync(SteamGame game, CancellationToken cancellationToken);
    void StartReading();
    void StopReading();
    event EventHandler<TelemetryEventArgs>? TelemetryReceived;
    event EventHandler<TelemetryConnectionStatus>? TelemetryStatusChanged;
}