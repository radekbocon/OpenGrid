using System.Collections.Generic;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using SimLab.Models.Telemetry;

namespace SimLab.Controls;

public partial class LapTableControl : UserControl
{
    public static readonly StyledProperty<IReadOnlyList<Lap>?> LapsProperty =
        AvaloniaProperty.Register<LapTableControl, IReadOnlyList<Lap>?>(nameof(Laps));

    public static readonly StyledProperty<Lap?> SelectedLapProperty =
        AvaloniaProperty.Register<LapTableControl, Lap?>(nameof(SelectedLap), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<ICommand?> DeleteLapCommandProperty =
        AvaloniaProperty.Register<LapTableControl, ICommand?>(nameof(DeleteLapCommand));

    public IReadOnlyList<Lap>? Laps
    {
        get => GetValue(LapsProperty);
        set => SetValue(LapsProperty, value);
    }

    public Lap? SelectedLap
    {
        get => GetValue(SelectedLapProperty);
        set => SetValue(SelectedLapProperty, value);
    }

    public ICommand? DeleteLapCommand
    {
        get => GetValue(DeleteLapCommandProperty);
        set => SetValue(DeleteLapCommandProperty, value);
    }

    public LapTableControl()
    {
        InitializeComponent();
    }
}
