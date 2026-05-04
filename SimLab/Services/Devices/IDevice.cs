using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimLab.Services.Devices;

public interface IDevice : IDisposable
{
    string Id { get; }
    string Name { get; }
    DeviceStatus Status { get; }
    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);
    void Disconnect();
}
