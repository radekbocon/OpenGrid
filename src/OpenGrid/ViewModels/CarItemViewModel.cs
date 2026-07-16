using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OpenGrid.ViewModels;

public partial class CarItemViewModel : ViewModelBase
{
    private readonly Action<CarItemViewModel> _onRemove;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? Description { get; set; }

    public CarItemViewModel(string name, string? description, Action<CarItemViewModel> onRemove)
    {
        Name = name;
        Description = description;
        _onRemove = onRemove;
    }

    [RelayCommand]
    private void Remove()
    {
        _onRemove.Invoke(this);
    }
}
