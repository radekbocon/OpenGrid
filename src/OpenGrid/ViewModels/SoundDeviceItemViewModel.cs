using Material.Icons;
using OpenGrid.Models;
using OpenGrid.Services.Devices;

namespace OpenGrid.ViewModels;

public partial class SoundDeviceItemViewModel : DeviceItemViewModel
{
    public override MaterialIconKind IconKind => MaterialIconKind.VolumeHigh;
    public SoundDeviceItemViewModel(IDeviceService deviceService, Action<DeviceItemViewModel> onRemove)
        : base(deviceService, onRemove)
    {
    }

    protected override void InitDevice(IDevice device)
    {
    }
}
