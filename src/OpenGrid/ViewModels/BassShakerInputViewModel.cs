using System.Collections.ObjectModel;
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

    public BassShakerInputSettings Settings { get; }
    public ObservableCollection<BassShakerMappingViewModel> Mappings { get; } = [];
    public TelemetryInput Input => Settings.Input;
    public string InputName => GetDisplayName(Settings.Input);
    public bool UsesWheelSelection => Input is TelemetryInput.WheelSpin or TelemetryInput.WheelLock;
    public bool HasMappings => Mappings.Count > 0;

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    public partial double Volume { get; set; }

    [ObservableProperty]
    public partial double Frequency { get; set; }

    [ObservableProperty]
    public partial double MinPercent { get; set; }

    [ObservableProperty]
    public partial double MaxPercent { get; set; }

    public static string GetDisplayName(TelemetryInput input) => input.GetDisplayName();

    public BassShakerInputViewModel(BassShakerInputSettings settings, Action onChanged, Action<BassShakerInputViewModel> onRemove)
    {
        Settings = settings;
        _onChanged = onChanged;
        _onRemove = onRemove;
        IsEnabled = settings.IsEnabled;
        Volume = settings.Volume * 100;
        Frequency = settings.Frequency;
        MinPercent = settings.MinPercent;
        MaxPercent = settings.MaxPercent;

        foreach (var mapping in settings.Mappings)
        {
            if (mapping is not null)
            {
                AddMappingViewModel(mapping);
            }
        }
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
    private void AddMapping()
    {
        var mapping = new BassShakerMapping();
        Settings.Mappings.Add(mapping);
        AddMappingViewModel(mapping);
        OnPropertyChanged(nameof(HasMappings));
        _onChanged();
    }

    private void AddMappingViewModel(BassShakerMapping mapping)
    {
        Mappings.Add(new BassShakerMappingViewModel(mapping, UsesWheelSelection, _onChanged, OnMappingRemoved));
    }

    private void OnMappingRemoved(BassShakerMappingViewModel mappingViewModel)
    {
        Settings.Mappings.Remove(mappingViewModel.Mapping);
        Mappings.Remove(mappingViewModel);
        OnPropertyChanged(nameof(HasMappings));
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
