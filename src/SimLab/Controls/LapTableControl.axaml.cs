using System.Collections.Generic;
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

    public LapTableControl()
    {
        InitializeComponent();
    }
}
