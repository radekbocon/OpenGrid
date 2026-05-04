using System.Collections.ObjectModel;
using System.IO.Ports;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using SimLab.Services.Devices;
using SimLab.Services.Devices.Moza;

namespace SimLab.ViewModels;

public partial class DevicesViewModel : ViewModelBase
{
    private readonly ITelemetryDispatcher _telemetryDispatcher;

    public DevicesViewModel(ITelemetryDispatcher telemetryDispatcher)
    {
        _telemetryDispatcher = telemetryDispatcher;
        Devices = new ObservableCollection<DeviceViewModel>();
        AvailablePorts = new ObservableCollection<string>(SerialPort.GetPortNames());
        AddMozaWheelCommand = new RelayCommand<string>(AddMozaWheel);
        ScanPortsCommand = new RelayCommand(ScanPorts);
    }

    public ObservableCollection<DeviceViewModel> Devices { get; }

    [ObservableProperty]
    private string? _selectedPort;

    public ObservableCollection<string> AvailablePorts { get; }

    public IRelayCommand<string> AddMozaWheelCommand { get; }
    public IRelayCommand ScanPortsCommand { get; }

    private void AddMozaWheel(string? portName)
    {
        if (string.IsNullOrEmpty(portName))
        {
            return;
        }

        var device = new MozaWheelDevice(portName);
        _telemetryDispatcher.RegisterDevice(device);
        Devices.Add(new DeviceViewModel(device));
        Log.Information("Added Moza wheel device on port {Port}", portName);
    }

    private void ScanPorts()
    {
        AvailablePorts.Clear();
        foreach (var port in SerialPort.GetPortNames())
        {
            AvailablePorts.Add(port);
        }
    }
}
