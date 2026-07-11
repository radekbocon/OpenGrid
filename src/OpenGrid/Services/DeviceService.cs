using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using OpenGrid.Models;

namespace OpenGrid.Services;

public sealed class DeviceService : IDeviceService
{
    private static readonly string DevicesPath = Path.Combine(
        Program.AppDataDirectory,
        "devices.json");

    public ObservableCollection<DeviceInfo> Devices { get; } = [];

    public DeviceService()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DevicesPath)!);
        Load();
    }

    public IReadOnlyList<DeviceInfo> ScanForDevices()
    {
        var found = new List<DeviceInfo>();

        ScanSerialPorts(found);
        ScanNetworkDevices(found);
        ScanDisplays(found);

        return found;
    }

    public void AddDevice(DeviceInfo device)
    {
        if (Devices.Any(d => d.Id == device.Id))
            return;

        Devices.Add(device);
        Save();
    }

    public void RemoveDevice(string deviceId)
    {
        var device = Devices.FirstOrDefault(d => d.Id == deviceId);
        if (device is not null)
        {
            Devices.Remove(device);
            Save();
        }
    }

    public DeviceInfo? GetById(string deviceId)
    {
        return Devices.FirstOrDefault(d => d.Id == deviceId);
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(DevicesPath);
            if (dir is not null)
                Directory.CreateDirectory(dir);

            File.WriteAllText(DevicesPath, JsonSerializer.Serialize(Devices.ToList()));
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Failed to save devices");
        }
    }

    public void Load()
    {
        try
        {
            if (!File.Exists(DevicesPath))
                return;

            var json = File.ReadAllText(DevicesPath);
            var devices = JsonSerializer.Deserialize<List<DeviceInfo>>(json);
            Devices.Clear();
            if (devices is not null)
            {
                foreach (var device in devices)
                    Devices.Add(device);
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Failed to load devices");
        }

        RefreshStatuses();
    }

    public void RefreshStatuses()
    {
        var serialPorts = new HashSet<string>(GetAvailableSerialPorts());
        var displayNames = new HashSet<string>(GetAvailableDisplayNames(), StringComparer.OrdinalIgnoreCase);

        foreach (var device in Devices)
        {
            device.Status = device.Type switch
            {
                DeviceType.Serial => serialPorts.Contains(device.ConnectionString)
                    ? DeviceConnectionStatus.Connected
                    : DeviceConnectionStatus.Disconnected,
                DeviceType.Display => displayNames.Contains(device.Name)
                    ? DeviceConnectionStatus.Connected
                    : DeviceConnectionStatus.Disconnected,
                DeviceType.Network => CheckNetworkDevice(device.ConnectionString),
                _ => DeviceConnectionStatus.Disconnected,
            };
        }
    }

    private static IEnumerable<string> GetAvailableSerialPorts()
    {
        try
        {
            return SerialPort.GetPortNames();
        }
        catch
        {
            return [];
        }
    }

    private static IEnumerable<string> GetAvailableDisplayNames()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return [];

        var screens = desktop.MainWindow?.Screens.All;
        if (screens is null)
            return [];

        return screens.Select(s => s.DisplayName).Where(n => !string.IsNullOrWhiteSpace(n))!;
    }

    private static DeviceConnectionStatus CheckNetworkDevice(string connectionString)
    {
        try
        {
            using var client = new System.Net.Sockets.TcpClient();
            var host = connectionString;
            var port = 80;

            if (connectionString.Contains(':'))
            {
                var parts = connectionString.Split(':');
                host = parts[0];
                if (int.TryParse(parts[1], out var p))
                    port = p;
            }

            var result = client.BeginConnect(host, port, null, null);
            var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(500));

            if (success && client.Connected)
                return DeviceConnectionStatus.Connected;

            return DeviceConnectionStatus.Disconnected;
        }
        catch
        {
            return DeviceConnectionStatus.Disconnected;
        }
    }

    private static void ScanSerialPorts(List<DeviceInfo> found)
    {
        try
        {
            var ports = SerialPort.GetPortNames();
            foreach (var port in ports)
            {
                found.Add(new DeviceInfo
                {
                    Id = $"serial-{port.ToLowerInvariant()}",
                    Name = port,
                    Type = DeviceType.Serial,
                    ConnectionString = port,
                    Description = "Serial port device"
                });
            }
        }
        catch
        {
            // Serial port enumeration not available on this platform
        }
    }

    private static void ScanNetworkDevices(List<DeviceInfo> found)
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var iface in interfaces)
            {
                if (iface.OperationalStatus != OperationalStatus.Up)
                    continue;

                var props = iface.GetIPProperties();
                foreach (var addr in props.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                        continue;

                    var ip = addr.Address.ToString();
                    if (ip == "127.0.0.1")
                        continue;

                    found.Add(new DeviceInfo
                    {
                        Id = $"network-{ip.Replace('.', '-')}",
                        Name = iface.Name,
                        Type = DeviceType.Network,
                        ConnectionString = ip,
                        Description = $"Network interface ({ip})"
                    });
                }
            }
        }
        catch
        {
            // Network enumeration not available
        }
    }

    private static void ScanDisplays(List<DeviceInfo> found)
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return;

            var screens = desktop.MainWindow?.Screens?.All;
            if (screens is null)
                return;

            foreach (var screen in screens)
            {
                var name = screen.DisplayName;
                if (string.IsNullOrWhiteSpace(name))
                    name = screen.IsPrimary ? "Primary Display" : "Display";

                var bounds = screen.Bounds;
                var connectionString = $"display://{screen.DisplayName}";
                var description = $"{bounds.Width}x{bounds.Height}";
                if (screen.Scaling != 1.0)
                    description += $" @ {screen.Scaling}x";

                found.Add(new DeviceInfo
                {
                    Id = $"display-{(screen.DisplayName ?? screen.IsPrimary.ToString()).ToLowerInvariant().Replace(' ', '-')}",
                    Name = name,
                    Type = DeviceType.Display,
                    ConnectionString = connectionString,
                    Description = description
                });
            }
        }
        catch
        {
            // Screen enumeration not available
        }
    }
}
