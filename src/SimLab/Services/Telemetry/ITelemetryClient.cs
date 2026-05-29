using System.Threading;
using System.Threading.Tasks;
using SimLab.Models;
using SimLab.Models.Telemetry;

namespace SimLab.Services.Telemetry;

public interface ITelemetryClient
{
    Task<bool> ConnectAsync(CancellationToken cancellationToken);
    void Stop();
    
    TelemetryRecord? ReadTelemetry();
}