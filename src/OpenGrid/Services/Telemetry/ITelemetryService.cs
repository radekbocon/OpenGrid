using System;
using System.Threading;
using System.Threading.Tasks;
using OpenGrid.Models;

namespace OpenGrid.Services.Telemetry;

public interface ITelemetryService : IDisposable
{
    TelemetryConnectionStatus ConnectionStatus { get; }
    SteamGame? CurrentGame { get; }
    Task<bool> ConnectAsync(SteamGame game);
    void Disconnect();
    event EventHandler<TelemetryEventArgs>? TelemetryReceived;
    event EventHandler<TelemetryConnectionStatus>? TelemetryStatusChanged;
}