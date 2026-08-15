using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    public static string GetDisplayName(TelemetryInput input)
    {
        return input switch
        {
            TelemetryInput.Abs => "ABS",
            TelemetryInput.Tc => "Traction Control",
            _ => input.ToString(),
        };
    }

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

    [RelayCommand]
    private void Remove()
    {
        _onRemove(this);
    }
}
