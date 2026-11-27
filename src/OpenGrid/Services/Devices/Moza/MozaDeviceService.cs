using System.IO.Ports;
using Avalonia.Media;
using OpenGrid.Models;
using OpenGrid.Models.Telemetry;
using OpenGrid.Services.Telemetry;
using Serilog;

namespace OpenGrid.Services.Devices.Moza;

public interface IMozaDeviceService : IDisposable
{
    event EventHandler<string>? StatusChanged;

    Task InitializeDevicesAsync();
    Task ConfigureDeviceAsync(MozaSerialDevice device);
    void RemoveDevice(string deviceId);
}

/// <summary>
/// Owns the serial connections to MOZA devices and drives the wheel RPM LEDs
/// from game telemetry when the user opted in.
/// </summary>
public sealed class MozaDeviceService : IMozaDeviceService
{
    private const int MinSendIntervalMs = 1000 / 30;

    private readonly ITelemetryService _telemetryService;
    private readonly IDeviceService _deviceService;
    private readonly ISettingsService _settingsService;
    private readonly IRpmLedEffectService _rpmLedEffectService;
    private readonly Dictionary<string, MozaConnection> _connections = [];
    private readonly SemaphoreSlim _sync = new(1, 1);

    private TelemetryRecord? _lastTelemetry;
    private SteamGame? _currentGame;
    private DateTime _lastSendTimestamp;
    private bool _disposed;

    public event EventHandler<string>? StatusChanged;

    public MozaDeviceService(
        ITelemetryService telemetryService,
        IDeviceService deviceService,
        ISettingsService settingsService,
        IRpmLedEffectService rpmLedEffectService)
    {
        _telemetryService = telemetryService;
        _deviceService = deviceService;
        _settingsService = settingsService;
        _rpmLedEffectService = rpmLedEffectService;

        _telemetryService.TelemetryReceived += OnTelemetryReceived;
        _telemetryService.TelemetryStatusChanged += OnTelemetryStatusChanged;
        _settingsService.SettingsChanged += OnSettingsChanged;
    }

    public async Task InitializeDevicesAsync()
    {
        try
        {
            await _sync.WaitAsync();

            if (_disposed)
            {
                return;
            }

            var activeDevices = _deviceService.GetPersistedDevices()
                .OfType<MozaSerialDevice>()
                .Where(device => device.IsRpmLedActive())
                .ToList();

            foreach (var deviceId in _connections.Keys.Except(activeDevices.Select(x => x.Id)).ToList())
            {
                RemoveConnection(deviceId);
            }

            foreach (var device in activeDevices)
            {
                await GetOrCreateConnectionAsync(device);
            }
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task ConfigureDeviceAsync(MozaSerialDevice device)
    {
        try
        {
            await _sync.WaitAsync();

            if (_disposed)
            {
                return;
            }

            Log.Debug(
                "Configuring MOZA device {DeviceId} ({Name}): enabled={Enabled}, leds={DriveLeds}, category={Category}",
                device.Id,
                device.Name,
                device.IsEnabled,
                device.DriveLedsWithTelemetry,
                device.Category);

            if (device.IsRpmLedActive())
            {
                await GetOrCreateConnectionAsync(device);
            }
            else
            {
                RemoveConnection(device.Id);
            }
        }
        finally
        {
            _sync.Release();
        }
    }

    public void RemoveDevice(string deviceId)
    {
        try
        {
            _sync.Wait();
            RemoveConnection(deviceId);
        }
        finally
        {
            _sync.Release();
        }
    }

    private async Task GetOrCreateConnectionAsync(MozaSerialDevice device)
    {
        await Task.Run(() =>
        {
            if (_connections.TryGetValue(device.Id, out var connection))
            {
                connection.UpdateDevice(device);
                connection.Open();
                SendLedsToConnection(connection);
                return;
            }

            connection = new MozaConnection(device, OnConnectionStatusChanged, GetInitialFrame);
            _connections[device.Id] = connection;
            connection.Open();
            SendLedsToConnection(connection);
        });
    }

    private IReadOnlyList<Color> GetInitialFrame(MozaSerialDevice device)
    {
        return _rpmLedEffectService.GetInitialLeds(device)
               ?? Enumerable.Repeat(RpmLedSegment.OffLed, device.RpmLedCount).ToList();
    }

    private void RemoveConnection(string deviceId)
    {
        if (!_connections.Remove(deviceId, out var connection))
        {
            return;
        }

        _rpmLedEffectService.Reset(deviceId);
        connection.Close();
        connection.Dispose();
    }

    private void OnConnectionStatusChanged(string deviceId)
    {
        StatusChanged?.Invoke(this, deviceId);
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        InitializeDevicesAsync().FireAndForgetSafe();
    }

    private void OnTelemetryReceived(object? sender, TelemetryEventArgs e)
    {
        try
        {
            _lastTelemetry = e.Telemetry;
            _currentGame = e.Game;

            var now = DateTime.Now;
            var minInterval = TimeSpan.FromMilliseconds(MinSendIntervalMs);
            if (now - _lastSendTimestamp < minInterval)
            {
                return;
            }

            _lastSendTimestamp = now;
            SendLeds();
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Error sending LEDs");
        }
    }

    private void OnTelemetryStatusChanged(object? sender, TelemetryConnectionStatus status)
    {
        Log.Debug("MOZA LED telemetry status changed to {Status}", status);

        if (status == TelemetryConnectionStatus.Connected)
        {
            return;
        }

        // Do not write an all-zero bitmask here. A zero frame switches the wheel
        // to the live LED pipeline with no lit LEDs, which is seen as all LEDs
        // going dark. Leaving the port alone lets the wheel keep/return to the
        // idle colours configured in Pit House or on the wheel itself.
        _lastTelemetry = null;
        _currentGame = null;
    }

    private void SendLeds()
    {
        List<MozaConnection> connections;
        try
        {
            _sync.Wait();
            connections = _connections.Values.ToList();
        }
        finally
        {
            _sync.Release();
        }

        foreach (var connection in connections)
        {
            SendLedsToConnection(connection);
        }
    }

    private void SendLedsToConnection(MozaConnection connection)
    {
        var snapshot = _rpmLedEffectService.Evaluate(connection.Device, _lastTelemetry, _currentGame);
        if (snapshot is null)
        {
            return;
        }

        connection.WriteFrame(snapshot, connection.Device.RpmLedCount);
    }


    public void Dispose()
    {
        try
        {
            _sync.Wait();

            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _telemetryService.TelemetryReceived -= OnTelemetryReceived;
            _telemetryService.TelemetryStatusChanged -= OnTelemetryStatusChanged;
            _settingsService.SettingsChanged -= OnSettingsChanged;

            foreach (var deviceId in _connections.Keys.ToList())
            {
                RemoveConnection(deviceId);
            }
        }
        finally
        {
            _sync.Release();
        }
    }

    private sealed class MozaConnection
    {
        private readonly SerialPort _port;
        private readonly Action<string> _statusChanged;
        private readonly Func<MozaSerialDevice, IReadOnlyList<Color>> _getInitialFrame;
        private readonly Lock _writeLock = new();
        private DeviceConnectionStatus _status = DeviceConnectionStatus.Disconnected;
        private byte[]? _lastLedFrame;
        private uint _lastActiveMask;
        private bool _ledsInitialized;
        private byte? _previousTelemetryMode;
        private string? _lastColorSignature;

        public MozaConnection(
            MozaSerialDevice device,
            Action<string> statusChanged,
            Func<MozaSerialDevice, IReadOnlyList<Color>> getInitialFrame)
        {
            Device = device;
            _statusChanged = statusChanged;
            _getInitialFrame = getInitialFrame;
            _port = new SerialPort(device.PortName, MozaProtocol.BaudRate)
            {
                ReadTimeout = 1000,
                WriteTimeout = 1000,
            };
        }

        public MozaSerialDevice Device { get; private set; }

        private bool IsDevicePresent() => MozaSerialPortEnumerator.IsPortPresent(Device.PortName);

        public void UpdateDevice(MozaSerialDevice device)
        {
            if (ReferenceEquals(Device, device))
            {
                return;
            }

            Device = device;
            if (_port.PortName != device.PortName)
            {
                Close();
                _port.PortName = device.PortName;
            }
        }

        public void Open()
        {
            lock (_writeLock)
            {
                if (!IsDevicePresent())
                {
                    ClosePort();
                    return;
                }

                OpenPort();
            }
        }

        public void WriteFrame(RpmLedEffectSnapshot snapshot, int totalLedCount)
        {
            lock (_writeLock)
            {
                // Do not try to open or write to a stale by-id path when the
                // device has been unplugged. ClosePort keeps the status in sync
                // and clears the LED state so a replugged wheel is initialized.
                if (!IsDevicePresent())
                {
                    ClosePort();
                    return;
                }

                try
                {
                    ApplyRpmColors(snapshot.Leds, totalLedCount);

                    var activeMask = snapshot.LightsOn ? BuildMask(snapshot.Leds) : 0u;

                    // At zero RPM, send the all-off frame only once when the bar
                    // drains. Continuous zero frames keep the wheel in the live LED
                    // pipeline with nothing lit, hiding the configured LED colours.
                    var repeatedIdleFrame = activeMask == 0 && _lastActiveMask == 0;
                    _lastActiveMask = activeMask;

                    if (repeatedIdleFrame)
                    {
                        return;
                    }

                    var mask = activeMask > ushort.MaxValue ? ushort.MaxValue : (ushort)activeMask;
                    var rpmFrame = MozaProtocol.BuildWheelRpmLedFrameLegacy(mask);

                    // Do not feed group 0x01 (button telemetry). The wheel switches its
                    // RGB buttons to the live buffer when it sees that frame, but this
                    // service only loads colours into the RPM group (0x00). With no
                    // group-1 colour data the buttons render black. OpenGrid owns the
                    // RPM strip only; leave button LEDs to the wheel / Pit House.

                    if (!rpmFrame.AsSpan().SequenceEqual(_lastLedFrame))
                    {
                        _lastLedFrame = rpmFrame;
                    }

                    if (!_port.IsOpen)
                    {
                        OpenPort();
                    }

                    if (_port.IsOpen)
                    {
                        _port.Write(rpmFrame, 0, rpmFrame.Length);
                    }
                }
                catch (Exception ex) when
                    (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
                {
                    Log.Warning(ex, "Failed to write to MOZA device {Device} on {Port}", Device.Name, Device.PortName);
                    ClosePort();
                }
            }
        }

        public void Close()
        {
            lock (_writeLock)
            {
                RestoreWheelTelemetryMode();
                ClosePort();
            }
        }

        public void Dispose()
        {
            _port.Dispose();
        }

        /// <summary>
        /// Replays the active frame's exact LED colours into the wheel's
        /// volatile live colour buffer. This is the boxflat startup step that
        /// makes the bitmask frames light up in the configured colours.
        /// </summary>
        private void ApplyRpmColors(IEnumerable<Color> leds, int totalLedCount)
        {
            if (!_port.IsOpen)
            {
                return;
            }

            var colors = RpmLedSegment.BuildColors(leds, totalLedCount);
            var signature = Convert.ToHexString(colors);
            if (signature == _lastColorSignature)
            {
                return;
            }

            var ledCount = colors.Length / 3;
            for (var start = 0; start < ledCount; start += 5)
            {
                WriteRaw(MozaProtocol.BuildWheelRpmColorChunkFrame(start, colors));
            }

            _lastColorSignature = signature;
        }

        public static uint BuildMask(IEnumerable<Color> leds)
        {
            uint mask = 0;
            var index = 0;

            foreach (var led in leds)
            {
                if (led != Colors.Black)
                {
                    mask |= 1u << index;
                }

                index++;
            }

            return mask;
        }

        /// <summary>
        /// The wheel's RPM live LED frame buffer is volatile. Match boxflat's
        /// startup order: all-off RPM frame, telemetry mode on, then the configured
        /// RPM colours. Only after that do the telemetry bitmasks light LEDs.
        /// The button group is deliberately not touched: feeding it would take over
        /// the RGB buttons without the matching group-1 colour data and go dark.
        /// </summary>
        private void InitializeWheelLeds()
        {
            if (_ledsInitialized)
            {
                return;
            }

            _ledsInitialized = true;

            try
            {
                var mode = ReadWheelValue(
                    MozaProtocol.BuildWheelTelemetryModeReadFrame(),
                    [MozaProtocol.WheelTelemetryModeCommand, MozaProtocol.WheelRpmLedGroup],
                    1);

                if (mode is { Length: 1 })
                {
                    _previousTelemetryMode = mode[0];
                }
                else
                {
                    Log.Debug("Could not read MOZA wheel telemetry mode on {Port}", Device.PortName);
                }

                WriteRaw(MozaProtocol.BuildWheelRpmLedFrameLegacy(0));

                if (_previousTelemetryMode is { } previousMode &&
                    previousMode != MozaProtocol.WheelTelemetryModeActive)
                {
                    WriteRaw(MozaProtocol.BuildWheelTelemetryModeWriteFrame(
                        MozaProtocol.WheelTelemetryModeActive));
                    Log.Debug(
                        "Switched MOZA wheel on {Port} to telemetry LED mode (was {PreviousMode})",
                        Device.PortName,
                        previousMode);
                }
                else if (_previousTelemetryMode is null)
                {
                    // Still request telemetry mode; some wheels don't answer
                    // the read but accept the write.
                    WriteRaw(MozaProtocol.BuildWheelTelemetryModeWriteFrame(
                        MozaProtocol.WheelTelemetryModeActive));
                    Log.Debug("Requested telemetry LED mode on {Port}", Device.PortName);
                }

                ApplyRpmColors(_getInitialFrame(Device), Device.RpmLedCount);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Failed to initialize MOZA wheel LEDs on {Port}", Device.PortName);
            }
        }

        private byte[]? ReadWheelValue(byte[] request, byte[] expectedPrefix, int valueLength)
        {
            var originalTimeout = _port.ReadTimeout;
            _port.ReadTimeout = 150;

            try
            {
                WriteRaw(request);

                var expectedGroup = MozaProtocol.ResponseGroup(request[2]);
                var expectedDevice = MozaProtocol.ResponseDevice(request[3]);
                var deadline = DateTime.Now + TimeSpan.FromMilliseconds(500);

                while (DateTime.Now < deadline)
                {
                    var frame = ReadFrame(deadline);
                    if (frame is null || frame.Length < 5 || frame[1] < expectedPrefix.Length + valueLength)
                    {
                        continue;
                    }

                    if (frame[2] != expectedGroup || frame[3] != expectedDevice)
                    {
                        continue;
                    }

                    var payload = frame.AsSpan(4, frame[1]);
                    if (!payload[..expectedPrefix.Length].SequenceEqual(expectedPrefix))
                    {
                        continue;
                    }

                    var value = payload.Slice(expectedPrefix.Length, valueLength).ToArray();
                    Log.Debug(
                        "MOZA wheel response on {Port}: {Frame}",
                        Device.PortName,
                        Convert.ToHexString(frame));
                    return value;
                }

                return null;
            }
            finally
            {
                _port.ReadTimeout = originalTimeout;
            }
        }

        private byte[]? ReadFrame(DateTime deadline)
        {
            int? pendingStart = null;

            while (DateTime.Now < deadline || pendingStart is not null)
            {
                var start = pendingStart ?? ReadByte();
                pendingStart = null;

                if (start != MozaProtocol.MessageStart)
                {
                    continue;
                }

                var length = ReadByte();
                if (length < 2 || length > 64)
                {
                    continue;
                }

                var frame = new byte[length + 5];
                frame[0] = MozaProtocol.MessageStart;
                frame[1] = (byte)length;

                var index = 2;
                while (index < frame.Length)
                {
                    var value = ReadByte();
                    if (value < 0)
                    {
                        return null;
                    }

                    if (value != MozaProtocol.MessageStart)
                    {
                        frame[index++] = (byte)value;
                        continue;
                    }

                    var next = ReadByte();
                    if (next == MozaProtocol.MessageStart)
                    {
                        frame[index++] = MozaProtocol.MessageStart;
                        continue;
                    }

                    // A lone 0x7E starts the next frame. Resynchronise with it.
                    pendingStart = next;
                    break;
                }

                if (index == frame.Length)
                {
                    return frame;
                }
            }

            return null;
        }

        private int ReadByte()
        {
            try
            {
                return _port.ReadByte();
            }
            catch (Exception ex) when (ex is TimeoutException or InvalidOperationException)
            {
                return -1;
            }
        }

        private void WriteRaw(byte[] frame)
        {
            if (!_port.IsOpen)
            {
                throw new InvalidOperationException("MOZA serial port is not open");
            }

            // The port can disappear between the WriteFrame presence check and
            // a later chunk/initialize write. Never write to a vanished device.
            if (!IsDevicePresent())
            {
                throw new InvalidOperationException("MOZA serial device is not connected");
            }

            _port.Write(frame, 0, frame.Length);
        }

        private void RestoreWheelTelemetryMode()
        {
            if (_previousTelemetryMode is not { } previousMode ||
                previousMode == MozaProtocol.WheelTelemetryModeActive ||
                !_port.IsOpen ||
                !IsDevicePresent())
            {
                return;
            }

            try
            {
                var frame = MozaProtocol.BuildWheelTelemetryModeWriteFrame(previousMode);
                _port.Write(frame, 0, frame.Length);
                Log.Debug(
                    "Restored MOZA wheel telemetry mode {Mode} on {Port}",
                    previousMode,
                    Device.PortName);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Failed to restore MOZA wheel telemetry mode on {Port}", Device.PortName);
            }
        }

        private void OpenPort()
        {
            if (!IsDevicePresent())
            {
                ClosePort();
                return;
            }

            try
            {
                if (_port.IsOpen)
                {
                    SetStatus(DeviceConnectionStatus.Connected);
                    return;
                }

                _port.Open();
                SetStatus(DeviceConnectionStatus.Connected);
                Log.Information("Connected to MOZA device {Device} on {Port}", Device.Name, Device.PortName);
                InitializeWheelLeds();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                Log.Warning(ex, "Failed to open MOZA serial port {Port}", Device.PortName);
                SetStatus(DeviceConnectionStatus.Disconnected);
            }
        }

        private void ClosePort()
        {
            try
            {
                if (_port.IsOpen)
                {
                    _port.Close();
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Failed to close MOZA serial port {Port}", Device.PortName);
            }

            // A disconnected port must be initialized from scratch on reconnect:
            // the wheel's live colour buffer is volatile.
            _lastLedFrame = null;
            _lastActiveMask = 0;
            _ledsInitialized = false;
            _previousTelemetryMode = null;
            _lastColorSignature = null;

            SetStatus(DeviceConnectionStatus.Disconnected);
        }

        private void SetStatus(DeviceConnectionStatus status)
        {
            if (_status == status)
            {
                return;
            }

            _status = status;
            Device.Status = status;
            _statusChanged(Device.Id);
        }
    }
}