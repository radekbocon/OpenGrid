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
    
    public string CarKey => _car.CarKey;
    
    public string Name { get; }

    [ObservableProperty]
    public partial int MaxRpm { get; set; }

    [ObservableProperty]
    public partial int RedlineRpm { get; set; }

    [ObservableProperty]
    public partial float BrakeBiasOffset { get; set; }

    public CarItemViewModel(CarProfile car, ICarConfigService carConfigService)
    {
        _car = car;
        _carConfigService = carConfigService;
        Name = car.Name;
        MaxRpm = car.MaxRpm;
        RedlineRpm = car.RedlineRpm;
        BrakeBiasOffset = car.BrakeBiasOffset;
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

    partial void OnBrakeBiasOffsetChanged(float value)
    {
        _car.BrakeBiasOffset = value;
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
