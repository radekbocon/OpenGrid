using SimLab.Models;

namespace SimLab.Services.Devices;

public interface ITelemetryDevice : IDevice
{
    void ProcessTelemetry(TelemetryRecord telemetry);
}
