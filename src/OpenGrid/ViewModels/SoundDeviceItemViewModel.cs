using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using OpenGrid.Models;
using OpenGrid.Services.Devices;
using OpenGrid.Services.Sound;

namespace OpenGrid.ViewModels;

public partial class SoundDeviceItemViewModel : DeviceItemViewModel
{
    private readonly IBassShakerService _bassShakerService;

    public override MaterialIconKind IconKind => MaterialIconKind.VolumeHigh;

    public ObservableCollection<BassShakerInputViewModel> BassShakerInputs { get; } = [];

    public ObservableCollection<TelemetryInput> AvailableTelemetryInputs { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedTelemetryInput))]
    [NotifyCanExecuteChangedFor(nameof(AddBassShakerInputCommand))]
    public partial TelemetryInput? SelectedTelemetryInput { get; set; }

    [ObservableProperty]
    public partial double Volume { get; set; }

    public bool HasSelectedTelemetryInput => SelectedTelemetryInput is not null;

    public bool HasBassShakerInputs => BassShakerInputs.Count > 0;

    public SoundDeviceItemViewModel(IDeviceService deviceService, IBassShakerService bassShakerService, Action<DeviceItemViewModel> onRemove)
        : base(deviceService, onRemove)
    {
        _bassShakerService = bassShakerService;
    }

    protected override void InitDevice(IDevice device)
    {
        BassShakerInputs.Clear();

        if (device is not SoundDevice soundDevice)
        {
            RefreshAvailableTelemetryInputs();
            return;
        }

        soundDevice.BassShaker ??= new BassShakerConfiguration();

        foreach (var inputSettings in soundDevice.BassShaker.Inputs)
        {
            AddBassShakerInputViewModel(inputSettings);
        }

        Volume = soundDevice.BassShaker.Volume * 100;
        RefreshAvailableTelemetryInputs();
        OnPropertyChanged(nameof(HasBassShakerInputs));
        ConfigureBassShaker();
    }

    partial void OnVolumeChanged(double value)
    {
        if (Device is not SoundDevice soundDevice || soundDevice.BassShaker is null)
            return;

        soundDevice.BassShaker.Volume = value / 100.0;
        DeviceService.SaveDevice(soundDevice);
        ConfigureBassShaker();
    }

    protected override void OnDeviceSettingsChanged()
    {
        ConfigureBassShaker();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedTelemetryInput))]
    private void AddBassShakerInput()
    {
        if (SelectedTelemetryInput is not { } input || Device is not SoundDevice soundDevice)
            return;

        soundDevice.BassShaker ??= new BassShakerConfiguration();

        var settings = new BassShakerInputSettings { Input = input };
        soundDevice.BassShaker.Inputs.Add(settings);
        AddBassShakerInputViewModel(settings);

        SelectedTelemetryInput = null;
        RefreshAvailableTelemetryInputs();
        OnPropertyChanged(nameof(HasBassShakerInputs));
        SaveAndConfigure();
    }

    private void AddBassShakerInputViewModel(BassShakerInputSettings settings)
    {
        BassShakerInputs.Add(new BassShakerInputViewModel(settings, OnBassShakerInputChanged, OnBassShakerInputRemoved));
    }

    private void OnBassShakerInputRemoved(BassShakerInputViewModel inputViewModel)
    {
        if (Device is not SoundDevice soundDevice || soundDevice.BassShaker is null)
            return;

        soundDevice.BassShaker.Inputs.Remove(inputViewModel.Settings);
        BassShakerInputs.Remove(inputViewModel);
        RefreshAvailableTelemetryInputs();
        OnPropertyChanged(nameof(HasBassShakerInputs));
        SaveAndConfigure();
    }

    private void OnBassShakerInputChanged()
    {
        SaveAndConfigure();
    }

    private void RefreshAvailableTelemetryInputs()
    {
        var configuredInputs = new HashSet<TelemetryInput>(BassShakerInputs.Select(x => x.Input));
        AvailableTelemetryInputs.Clear();
        foreach (var input in Enum.GetValues<TelemetryInput>())
        {
            if (!configuredInputs.Contains(input))
            {
                AvailableTelemetryInputs.Add(input);
            }
        }

        if (SelectedTelemetryInput is not null && !configuredInputs.Contains(SelectedTelemetryInput.Value))
        {
            SelectedTelemetryInput = null;
        }
    }

    private void SaveAndConfigure()
    {
        if (Device is not SoundDevice soundDevice)
            return;

        DeviceService.SaveDevice(soundDevice);
        ConfigureBassShaker();
    }

    private void ConfigureBassShaker()
    {
        if (Device is not SoundDevice soundDevice)
            return;

        _bassShakerService.ConfigureDevice(soundDevice);
    }
}
