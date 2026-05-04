using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimLab.Services.Devices;

namespace SimLab.ViewModels;

public partial class DeviceViewModel : ObservableObject
{
    private readonly IDevice _device;

    public DeviceViewModel(IDevice device)
    {
        _device = device;
        Name = device.Name;
        Status = device.Status;
        ConnectCommand = new AsyncRelayCommand(ConnectAsync);
        DisconnectCommand = new RelayCommand(Disconnect);
    }

    public string Id => _device.Id;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private DeviceStatus _status;

    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }

    private async Task ConnectAsync()
    {
        await _device.ConnectAsync();
        Status = _device.Status;
    }

    private void Disconnect()
    {
        _device.Disconnect();
        Status = _device.Status;
    }
}
