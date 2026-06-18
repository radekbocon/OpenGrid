using Avalonia;
using Avalonia.Controls;
using OpenGrid.Models.Telemetry;

namespace OpenGrid.Controls;

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
