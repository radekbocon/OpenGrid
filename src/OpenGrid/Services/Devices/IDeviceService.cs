using OpenGrid.Models;

namespace OpenGrid.Services.Devices;

public interface IDeviceService
{
    IReadOnlyList<IDevice> GetSavedDevices();
    IReadOnlyList<IDevice> ScanForDevices();
    void AddDevice(IDevice device);
    void SaveDevice(IDevice device);
    void RemoveDevice(IDevice device);
}
