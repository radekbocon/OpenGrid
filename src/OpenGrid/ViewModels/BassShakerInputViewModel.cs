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
    private bool _isLoading;

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
        _isLoading = true;
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

        _isLoading = false;
    }

    partial void OnIsEnabledChanged(bool value)
    {
        Settings.IsEnabled = value;
        NotifyChanged();
    }

    partial void OnVolumeChanged(double value)
    {
        Settings.Volume = value / 100.0;
        NotifyChanged();
    }

    partial void OnFrequencyChanged(double value)
    {
        Settings.Frequency = value;
        NotifyChanged();
    }

    partial void OnMinPercentChanged(double value)
    {
        Settings.MinPercent = value;
        NotifyChanged();
    }

    partial void OnMaxPercentChanged(double value)
    {
        Settings.MaxPercent = value;
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        if (!_isLoading)
        {
            _onChanged();
        }
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
