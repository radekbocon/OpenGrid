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
    
    public string Name { get; }

    [ObservableProperty]
    public partial int MaxRpm { get; set; }

    [ObservableProperty]
    public partial int RedlineRpm { get; set; }

    public CarItemViewModel(CarProfile car, ICarConfigService carConfigService, Action<CarItemViewModel> onRemove)
    {
        _car = car;
        _carConfigService = carConfigService;
        _onRemove = onRemove;
        Name = car.Name;
        MaxRpm = car.MaxRpm;
        RedlineRpm = car.RedlineRpm;
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
            _carConfigService.Remove(_car.Id);
            _onRemove.Invoke(this);
        }
    }

    private void Save()
    {
        _carConfigService.Save(_car);
    }
}
