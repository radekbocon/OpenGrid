using Avalonia;
using Avalonia.Controls;
using SimLab.Models.Telemetry;

namespace SimLab.Controls;

public partial class LapStatsControl : UserControl
{
    public static readonly StyledProperty<Lap?> SelectedLapProperty =
        AvaloniaProperty.Register<LapStatsControl, Lap?>(nameof(SelectedLap));

    public Lap? SelectedLap
    {
        get => GetValue(SelectedLapProperty);
        set => SetValue(SelectedLapProperty, value);
    }

    public LapStatsControl()
    {
        InitializeComponent();
    }
}
