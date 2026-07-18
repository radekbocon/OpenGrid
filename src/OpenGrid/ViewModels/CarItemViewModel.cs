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
    private float _brakeBiasOffset;

    public string CarKey => _car.CarKey;
    
    public string Name { get; }

    [ObservableProperty]
    public partial int MaxRpm { get; set; }

    [ObservableProperty]
    public partial int RedlineRpm { get; set; }

    public float BrakeBiasOffsetPercentage
    {
        get => _brakeBiasOffset * 100;
        set
        {
            SetProperty(ref _brakeBiasOffset, value / 100);
            _car.BrakeBiasOffset = _brakeBiasOffset;
            Save();
        }
    }

    public CarItemViewModel(CarProfile car, ICarConfigService carConfigService)
    {
        _car = car;
        _carConfigService = carConfigService;
        Name = car.Name;
        MaxRpm = car.MaxRpm;
        RedlineRpm = car.RedlineRpm;
        _brakeBiasOffset = car.BrakeBiasOffset;
    }

    partial void OnMaxRpmChanged(int value)
    {
        _car.MaxRpm = value;
        Save();
    }

    partial void OnRedlineRpmChanged(int value)
    {
        _car.RedlineRpm = value;
        Save();
    }

    [RelayCommand]
    private async Task RemoveAsync()
    {
        var confirmDialog = new ConfirmDialog($"Are you sure you want to remove '{Name}'?");
        await DialogHost.Show(confirmDialog);

        if (confirmDialog.Result)
        {
            _carConfigService.Remove(_car.CarKey);
        }
    }

    private void Save()
    {
        _carConfigService.Save(_car);
    }
}
