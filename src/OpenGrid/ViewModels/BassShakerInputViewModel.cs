using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using OpenGrid.Controls;
using OpenGrid.Models;

namespace OpenGrid.ViewModels;

public partial class BassShakerInputViewModel : ViewModelBase
{
    private readonly Action _onChanged;
    private readonly Action<BassShakerInputViewModel> _onRemove;

    public static IReadOnlyList<BassShakerChannel> AvailableChannels { get; } = Enum.GetValues<BassShakerChannel>();

    public BassShakerInputSettings Settings { get; }
    public TelemetryInput Input => Settings.Input;
    public string InputName => GetDisplayName(Settings.Input);
    public bool UsesWheelSelection => Input is TelemetryInput.WheelSpin or TelemetryInput.WheelLock;

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    public partial BassShakerChannel Channel { get; set; }

    [ObservableProperty]
    public partial double Volume { get; set; }

    [ObservableProperty]
    public partial double Frequency { get; set; }

    [ObservableProperty]
    public partial double MinPercent { get; set; }

    [ObservableProperty]
    public partial double MaxPercent { get; set; }

    [ObservableProperty]
    public partial bool IsFrontLeftSelected { get; set; }

    [ObservableProperty]
    public partial bool IsFrontRightSelected { get; set; }

    [ObservableProperty]
    public partial bool IsRearLeftSelected { get; set; }

    [ObservableProperty]
    public partial bool IsRearRightSelected { get; set; }

    public static string GetDisplayName(TelemetryInput input) => input.GetDisplayName();

    public BassShakerInputViewModel(BassShakerInputSettings settings, Action onChanged, Action<BassShakerInputViewModel> onRemove)
    {
        Settings = settings;
        _onChanged = onChanged;
        _onRemove = onRemove;
        IsEnabled = settings.IsEnabled;
        Channel = settings.Channel;
        Volume = settings.Volume * 100;
        Frequency = settings.Frequency;
        MinPercent = settings.MinPercent;
        MaxPercent = settings.MaxPercent;
        IsFrontLeftSelected = settings.Wheels.Includes(WheelSelection.FrontLeft);
        IsFrontRightSelected = settings.Wheels.Includes(WheelSelection.FrontRight);
        IsRearLeftSelected = settings.Wheels.Includes(WheelSelection.RearLeft);
        IsRearRightSelected = settings.Wheels.Includes(WheelSelection.RearRight);
    }

    partial void OnIsEnabledChanged(bool value)
    {
        Settings.IsEnabled = value;
        _onChanged();
    }

    partial void OnChannelChanged(BassShakerChannel value)
    {
        Settings.Channel = value;
        _onChanged();
    }

    partial void OnVolumeChanged(double value)
    {
        Settings.Volume = value / 100.0;
        _onChanged();
    }

    partial void OnFrequencyChanged(double value)
    {
        Settings.Frequency = value;
        _onChanged();
    }

    partial void OnMinPercentChanged(double value)
    {
        Settings.MinPercent = value;
        _onChanged();
    }

    partial void OnMaxPercentChanged(double value)
    {
        Settings.MaxPercent = value;
        _onChanged();
    }

    partial void OnIsFrontLeftSelectedChanged(bool value) => SetWheel(WheelSelection.FrontLeft, value);

    partial void OnIsFrontRightSelectedChanged(bool value) => SetWheel(WheelSelection.FrontRight, value);

    partial void OnIsRearLeftSelectedChanged(bool value) => SetWheel(WheelSelection.RearLeft, value);

    partial void OnIsRearRightSelectedChanged(bool value) => SetWheel(WheelSelection.RearRight, value);

    private void SetWheel(WheelSelection wheel, bool selected)
    {
        Settings.Wheels = selected ? Settings.Wheels | wheel : Settings.Wheels & ~wheel;
        _onChanged();
    }

    [RelayCommand]
    private async Task RemoveAsync()
    {
        var confirmDialog = new ConfirmDialog("Remove this effect?");
        await DialogHost.Show(confirmDialog);

        if (!confirmDialog.Result)
        {
            return;
        }
        
        _onRemove(this);
    }
}
