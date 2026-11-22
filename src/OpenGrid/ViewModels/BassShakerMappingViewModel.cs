using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenGrid.Models;

namespace OpenGrid.ViewModels;

public partial class BassShakerMappingViewModel : ViewModelBase
{
    private readonly Action _onChanged;
    private readonly Action<BassShakerMappingViewModel> _onRemove;
    private bool _isLoading;

    public static IReadOnlyList<BassShakerChannel> AvailableChannels { get; } = Enum.GetValues<BassShakerChannel>();

    public BassShakerMapping Mapping { get; }
    public bool UsesWheelSelection { get; }

    [ObservableProperty]
    public partial BassShakerChannel Channel { get; set; }

    [ObservableProperty]
    public partial bool IsFrontLeftSelected { get; set; }

    [ObservableProperty]
    public partial bool IsFrontRightSelected { get; set; }

    [ObservableProperty]
    public partial bool IsRearLeftSelected { get; set; }

    [ObservableProperty]
    public partial bool IsRearRightSelected { get; set; }

    public BassShakerMappingViewModel(
        BassShakerMapping mapping,
        bool usesWheelSelection,
        Action onChanged,
        Action<BassShakerMappingViewModel> onRemove)
    {
        Mapping = mapping;
        UsesWheelSelection = usesWheelSelection;
        _onChanged = onChanged;
        _onRemove = onRemove;

        _isLoading = true;
        Channel = mapping.Channel;
        IsFrontLeftSelected = mapping.Wheels.Includes(WheelSelection.FrontLeft);
        IsFrontRightSelected = mapping.Wheels.Includes(WheelSelection.FrontRight);
        IsRearLeftSelected = mapping.Wheels.Includes(WheelSelection.RearLeft);
        IsRearRightSelected = mapping.Wheels.Includes(WheelSelection.RearRight);
        _isLoading = false;
    }

    partial void OnChannelChanged(BassShakerChannel value)
    {
        Mapping.Channel = value;
        NotifyChanged();
    }

    partial void OnIsFrontLeftSelectedChanged(bool value) => SetWheel(WheelSelection.FrontLeft, value);

    partial void OnIsFrontRightSelectedChanged(bool value) => SetWheel(WheelSelection.FrontRight, value);

    partial void OnIsRearLeftSelectedChanged(bool value) => SetWheel(WheelSelection.RearLeft, value);

    partial void OnIsRearRightSelectedChanged(bool value) => SetWheel(WheelSelection.RearRight, value);

    private void SetWheel(WheelSelection wheel, bool selected)
    {
        Mapping.Wheels = selected ? Mapping.Wheels | wheel : Mapping.Wheels & ~wheel;
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
    private void Remove()
    {
        _onRemove(this);
    }
}
