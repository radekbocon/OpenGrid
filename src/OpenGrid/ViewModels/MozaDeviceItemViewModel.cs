using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using OpenGrid.Models;
using OpenGrid.Services.Devices;
using OpenGrid.Services.Devices.Moza;

namespace OpenGrid.ViewModels;

public partial class MozaDeviceItemViewModel : DeviceItemViewModel
{
    private readonly IMozaDeviceService _mozaDeviceService;
    private bool _isInitializing;
    private bool _isInitialized;

    public override MaterialIconKind IconKind => MaterialIconKind.Gauge;

    public IReadOnlyList<int> FrameLengthOptions { get; } =
        Enumerable.Range(1, RpmLedSegment.MaxLeds).ToArray();

    public string PortName => (Device as MozaSerialDevice)?.PortName ?? "-";

    public string SerialNumber => (Device as MozaSerialDevice)?.SerialNumber ?? "-";

    public string Category => (Device as MozaSerialDevice)?.Category.ToString() ?? "-";

    public ObservableCollection<MozaRpmSegmentViewModel> RpmSegments { get; } = [];

    [ObservableProperty]
    public partial bool DriveLedsWithTelemetry { get; set; }

    [ObservableProperty]
    public partial int FrameLength { get; set; } = 10;

    public MozaDeviceItemViewModel(
        IDeviceService deviceService,
        IMozaDeviceService mozaDeviceService,
        Action<DeviceItemViewModel> onRemove)
        : base(deviceService, onRemove)
    {
        _mozaDeviceService = mozaDeviceService;
    }

    protected override void InitDevice(IDevice device)
    {
        _isInitializing = true;

        if (device is MozaSerialDevice serialDevice)
        {
            DriveLedsWithTelemetry = serialDevice.DriveLedsWithTelemetry;
            FrameLength = Math.Clamp(serialDevice.RpmLedCount, 1, RpmLedSegment.MaxLeds);
            serialDevice.RpmLedCount = FrameLength;

            if (serialDevice.RpmSegments.Count == 0)
            {
                serialDevice.RpmSegments = RpmLedSegment.CreateDefault(FrameLength);
            }

            RpmSegments.Clear();
            foreach (var segment in RpmLedSegment.Normalize(serialDevice.RpmSegments, FrameLength))
            {
                RpmSegments.Add(new MozaRpmSegmentViewModel(
                    segment,
                    FrameLength,
                    OnRpmSegmentChanged,
                    RemoveRpmSegment));
            }

            ReindexSegments();
            _mozaDeviceService.ConfigureDevice(serialDevice);
        }
        else
        {
            DriveLedsWithTelemetry = false;
        }

        OnPropertyChanged(nameof(PortName));
        OnPropertyChanged(nameof(SerialNumber));
        OnPropertyChanged(nameof(Category));

        _isInitializing = false;
        _isInitialized = true;
    }

    partial void OnDriveLedsWithTelemetryChanged(bool value)
    {
        SaveAndConfigure();
    }

    partial void OnFrameLengthChanged(int value)
    {
        if (_isInitializing || !_isInitialized)
        {
            return;
        }

        foreach (var segment in RpmSegments)
        {
            segment.Resize(FrameLength);
        }

        SaveAndConfigure();
    }

    protected override void OnDeviceSettingsChanged()
    {
        SaveAndConfigure();
    }

    [RelayCommand]
    private void AddRpmSegment()
    {
        var frame = new RpmLedSegment(
            Enumerable.Repeat(RpmLedSegment.OffLed, FrameLength),
            100);

        RpmSegments.Add(new MozaRpmSegmentViewModel(
            frame,
            FrameLength,
            OnRpmSegmentChanged,
            RemoveRpmSegment));

        ReindexSegments();
        SaveAndConfigure();
    }

    private void RemoveRpmSegment(MozaRpmSegmentViewModel segment)
    {
        if (RpmSegments.Count <= 1 || !RpmSegments.Remove(segment))
        {
            return;
        }

        ReindexSegments();
        SaveAndConfigure();
    }

    private void OnRpmSegmentChanged()
    {
        SaveAndConfigure();
    }

    private void ReindexSegments()
    {
        for (var i = 0; i < RpmSegments.Count; i++)
        {
            RpmSegments[i].Index = i + 1;
        }
    }

    private void SaveAndConfigure()
    {
        // DeviceItemViewModel.Init sets IsEnabled before InitDevice runs, which
        // raises OnDeviceSettingsChanged. Ignore that callback until the VM has
        // loaded the persisted values, otherwise the defaults overwrite them.
        if (!_isInitialized || _isInitializing || Device is not MozaSerialDevice serialDevice)
        {
            return;
        }

        serialDevice.DriveLedsWithTelemetry = DriveLedsWithTelemetry;
        serialDevice.RpmLedCount = FrameLength;
        serialDevice.RpmSegments = RpmSegments.Select(x => x.ToModel()).ToList();
        DeviceService.SaveDevice(serialDevice);
        _mozaDeviceService.ConfigureDevice(serialDevice);
    }
}
