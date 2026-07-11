using System.Collections.ObjectModel;
using OpenGrid.Models;

namespace OpenGrid.Services;

public interface IDeviceService
{
    ObservableCollection<DeviceInfo> Devices { get; }
    IReadOnlyList<DeviceInfo> ScanForDevices();
    void AddDevice(DeviceInfo device);
    void RemoveDevice(string deviceId);
    DeviceInfo? GetById(string deviceId);
    void RefreshStatuses();
    void Save();
    void Load();
}
