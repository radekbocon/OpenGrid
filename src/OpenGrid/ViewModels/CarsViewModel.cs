using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OpenGrid.ViewModels;

public partial class CarsViewModel : ViewModelBase
{
    public ObservableCollection<CarItemViewModel> Cars { get; }

    public bool HasCars => Cars.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCarSelected))]
    [NotifyPropertyChangedFor(nameof(HasCars))]
    public partial CarItemViewModel? SelectedCar { get; set; }

    public bool IsCarSelected => SelectedCar is not null;

    public CarsViewModel()
    {
        IsMenuItem = true;
        Cars = [];
    }
}
