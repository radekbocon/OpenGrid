using System.Text.Json;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using OpenGrid.Models;
using Serilog;

namespace OpenGrid.Services.Devices;

public sealed class DeviceService : IDeviceService
{
    private static readonly string DevicesDirectory = Path.Combine(
        Program.AppDataDirectory,
        "devices");
    
    private readonly Dictionary<DeviceType, IDeviceSerializer> _serializers;

    private List<IDevice> _devices = [];

    public DeviceService()
    {
        Directory.CreateDirectory(DevicesDirectory);
        _serializers = new Dictionary<DeviceType, IDeviceSerializer>
        {
            { DeviceType.Display, new DisplayDeviceSerializer() },
            { DeviceType.Sound, new SoundDeviceSerializer() },
        };
        GetPersistedDevices();
    }

    public IReadOnlyList<IDevice> GetPersistedDevices()
    {
        var screens = EnumerateScreens();
        var soundDevices = EnumerateSoundDevices();
        var persistedDevices = LoadPersistedDevices();

        foreach (var device in persistedDevices)
        {
            switch (device)
            {
                case DisplayDevice displayDevice:
                    displayDevice.Screen = screens.FirstOrDefault(x => x.DisplayName == device.Id);
                    break;
                case SoundDevice soundDevice:
                    soundDevice.Status = soundDevices.Any(x => x.Id == device.Id)
                        ? DeviceConnectionStatus.Connected
                        : DeviceConnectionStatus.Disconnected;
                    break;
            }
        }
        
        _devices = persistedDevices;
        return persistedDevices;
    }

    public IReadOnlyList<IDevice> ScanForDevices()
    {
        var screens = EnumerateScreens();
        var soundDevices = EnumerateSoundDevices();
        var saved = LoadPersistedDevices();
        var savedIds = new HashSet<string>(saved.Select(d => d.Id));
        var newDevices = new List<IDevice>();

        foreach (var screen in screens)
        {
            if (savedIds.Contains(screen.DisplayName!))
            {
                continue;
            }

            var device = new DisplayDevice
            {
                Id = screen.DisplayName!,
                DeviceType = DeviceType.Display,
                Description = $"{screen.Bounds.Width}x{screen.Bounds.Height}",
                IsEnabled = true,
                Screen = screen,
                Name = screen.DisplayName!,
            };
            newDevices.Add(device);
        }

        foreach (var soundDevice in soundDevices)
        {
            if (savedIds.Contains(soundDevice.Id))
            {
                continue;
            }

            var device = new SoundDevice
            {
                Id = soundDevice.Id,
                DeviceType = DeviceType.Sound,
                Name = soundDevice.Name,
                Description = soundDevice.Description,
                IsEnabled = true,
            };
            newDevices.Add(device);
        }

        return newDevices;
    }

    public void AddDevice(IDevice device)
    {
        if (_devices.Any(d => d.Id == device.Id))
            return;

        _devices.Add(device);
        SaveDevice(device);
    }

    public void SaveDevice(IDevice device)
    {
        SaveDeviceFile(device);
    }

    public void RemoveDevice(IDevice device)
    {
        _devices.RemoveAll(d => d.Id == device.Id);
        DeleteDeviceFile(device);
    }

    private List<IDevice> LoadPersistedDevices()
    {
        var devices = new List<IDevice>();
        foreach (var file in Directory.GetFiles(DevicesDirectory, "*.json"))
        {
            try
            {
                var text = File.ReadAllText(file);
                var json = JsonDocument.Parse(text);
                var deviceType = (DeviceType)json.RootElement.GetProperty("DeviceType").GetInt32();
                var serializer = _serializers[deviceType];
                var device = serializer.FromJson(text);
                if (device is not null)
                {
                    devices.Add(device);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to load device file {Path}", file);
            }
        }
        
        return devices;
    }

    private static string GetDevicePath(IDevice device)
    {
        return Path.Combine(DevicesDirectory, $"{device.Id}.json");
    }

    private void SaveDeviceFile(IDevice device)
    {
        try
        {
            var json = _serializers[device.DeviceType].ToJson(device);
            File.WriteAllText(GetDevicePath(device), json);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to save device {DeviceId}", device.Id);
        }
    }

    private static void DeleteDeviceFile(IDevice device)
    {
        try
        {
            var path = GetDevicePath(device);
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to delete device {DeviceId}", device.Id);
        }
    }


    private static List<Screen> EnumerateScreens()
    {
        return Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
               desktop.MainWindow?.Screens.All is not { } screens
            ? []
            : screens.Where(x => x.DisplayName is not null).ToList();
    }

    private static List<SoundDeviceInfo> EnumerateSoundDevices()
    {
        try
        {
            var enumerator = new PipeWireDeviceEnumerator();
            return enumerator.EnumerateDevices();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to enumerate sound devices");
            return [];
        }
    }
}