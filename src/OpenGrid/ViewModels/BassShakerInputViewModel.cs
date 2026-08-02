using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenGrid.Models;

namespace OpenGrid.ViewModels;

public partial class BassShakerInputViewModel : ViewModelBase
{
    private readonly Action _onChanged;
    private readonly Action<BassShakerInputViewModel> _onRemove;

    public BassShakerInputSettings Settings { get; }
    public TelemetryInput Input => Settings.Input;
    public string InputName => GetDisplayName(Settings.Input);

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    public partial double Volume { get; set; }

    [ObservableProperty]
    public partial double Frequency { get; set; }

    [ObservableProperty]
    public partial double MinRpmPercent { get; set; }

    [ObservableProperty]
    public partial double MaxRpmPercent { get; set; }

    public static string GetDisplayName(TelemetryInput input)
    {
        return input switch
        {
            TelemetryInput.EngineRpm => "Engine RPM",
            _ => input.ToString(),
        };
    }

    public BassShakerInputViewModel(BassShakerInputSettings settings, Action onChanged, Action<BassShakerInputViewModel> onRemove)
    {
        Settings = settings;
        _onChanged = onChanged;
        _onRemove = onRemove;
        IsEnabled = settings.IsEnabled;
        Volume = settings.Volume * 100;
        Frequency = settings.Frequency;
        MinRpmPercent = settings.MinRpmPercent;
        MaxRpmPercent = settings.MaxRpmPercent;
    }

    partial void OnIsEnabledChanged(bool value)
    {
        Settings.IsEnabled = value;
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

    partial void OnMinRpmPercentChanged(double value)
    {
        Settings.MinRpmPercent = value;
        _onChanged();
    }

    partial void OnMaxRpmPercentChanged(double value)
    {
        Settings.MaxRpmPercent = value;
        _onChanged();
    }

    [RelayCommand]
    private void Remove()
    {
        _onRemove(this);
    }
}
