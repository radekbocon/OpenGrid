using OpenGrid.Models;

namespace OpenGrid.ViewModels;

public sealed class CarPickerOption
{
    public CarPickerOption(CarProfile car)
    {
        Car = car;
        DisplayName = car.Name;
    }

    public CarProfile Car { get; }
    public string DisplayName { get; }

    public override string ToString() => DisplayName;
}
