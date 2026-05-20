using System.Threading;
using System.Threading.Tasks;
using SimLab.Models;

namespace SimLab.Services.Telemetry;

public interface ITelemetryClient
{
    Task<bool> ConnectAsync(CancellationToken cancellationToken);
    void Stop();
    
    TelemetryRecord? ReadTelemetry();
}