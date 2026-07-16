using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using OpenGrid.Controls;
using OpenGrid.Models;
using OpenGrid.Services;

namespace OpenGrid.ViewModels;

public partial class CarItemViewModel : ViewModelBase
{
    private readonly CarProfile _car;
    private readonly ICarConfigService _carConfigService;
    private readonly Action<CarItemViewModel> _onRemove;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int MaxRpm { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RedlinePercentage))]
    public partial int RedlineRpm { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RedlineRpm))]
    public partial double RedlinePercentage { get; set; }

    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "New Car" : Name;

    public CarItemViewModel(CarProfile car, ICarConfigService carConfigService, Action<CarItemViewModel> onRemove)
    {
        _car = car;
        _carConfigService = carConfigService;
        _onRemove = onRemove;
        Name = car.Name;
        MaxRpm = car.MaxRpm;
        RedlineRpm = car.RedlineRpm;
        RedlinePercentage = car.RedlinePercentage;
    }

    partial void OnNameChanged(string value)
    {
        _car.Name = value;
        OnPropertyChanged(nameof(DisplayName));
        Save();
    }

    partial void OnMaxRpmChanged(int value)
    {
        _car.MaxRpm = value;
        Save();
    }

    partial void OnRedlineRpmChanged(int value)
    {
        _car.RedlineRpm = value;
        RedlinePercentage = _car.RedlinePercentage;
        Save();
    }

    partial void OnRedlinePercentageChanged(double value)
    {
        _car.RedlinePercentage = value;
        RedlineRpm = _car.RedlineRpm;
        Save();
    }

    [RelayCommand]
    private async Task RemoveAsync()
    {
        var confirmDialog = new ConfirmDialog($"Are you sure you want to remove '{DisplayName}'?");
        await DialogHost.Show(confirmDialog);

        if (confirmDialog.Result)
        {
            _carConfigService.Remove(_car.Id);
            _onRemove.Invoke(this);
        }
    }

    private void Save()
    {
        _carConfigService.Save(_car);
    }
}
