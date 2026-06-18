using System.Threading;
using System.Threading.Tasks;
using OpenGrid.Models;
using OpenGrid.Models.Telemetry;

namespace OpenGrid.Services.Telemetry;

public interface ITelemetryClient
{
    Task<bool> ConnectAsync(CancellationToken cancellationToken);
    void Stop();
    
    TelemetryRecord? ReadTelemetry();
}