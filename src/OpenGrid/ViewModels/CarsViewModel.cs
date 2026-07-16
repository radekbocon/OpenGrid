using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenGrid.Models;
using OpenGrid.Services;

namespace OpenGrid.ViewModels;

public partial class CarsViewModel : ViewModelBase
{
    private readonly ICarConfigService _carConfigService;

    public ObservableCollection<CarItemViewModel> Cars { get; }

    public bool HasCars => Cars.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCarSelected))]
    [NotifyPropertyChangedFor(nameof(HasCars))]
    public partial CarItemViewModel? SelectedCar { get; set; }

    public bool IsCarSelected => SelectedCar is not null;

    public CarsViewModel(ICarConfigService carConfigService)
    {
        _carConfigService = carConfigService;
        IsMenuItem = true;
        Cars = [];
    }

    protected override Task OnLoadedAsync()
    {
        _carConfigService.CarAdded += OnCarConfigAdded;
        
        Cars.Clear();
        foreach (var car in _carConfigService.GetAll())
        {
            Cars.Add(new CarItemViewModel(car, _carConfigService, OnRemove));
        }
        OnPropertyChanged(nameof(HasCars));
        SelectedCar = Cars.FirstOrDefault();
        return base.OnLoadedAsync();
    }
    
    protected override Task OnUnloadedAsync()
    {
        _carConfigService.CarAdded -= OnCarConfigAdded;
        return base.OnUnloadedAsync();
    }

    private void OnCarConfigAdded(object? sender, CarProfile car)
    {
        var item = new CarItemViewModel(car, _carConfigService, OnRemove);
        Cars.Add(item);
        OnPropertyChanged(nameof(HasCars));
        if (SelectedCar is null)
        {
            SelectedCar = item;
        }
    }

    private void OnRemove(CarItemViewModel item)
    {
        Cars.Remove(item);
        SelectedCar = Cars.FirstOrDefault();
        OnPropertyChanged(nameof(HasCars));
        OnPropertyChanged(nameof(IsCarSelected));
    }
}
