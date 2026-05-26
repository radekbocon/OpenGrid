using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ScottPlot.Avalonia;
using SimLab.Controls;
using SimLab.ViewModels;

namespace SimLab.Views;

public partial class SessionDetailsView : UserControl
{
    private readonly ChartInteractionHelper _interactionHelper;

    public SessionDetailsView()
    {
        InitializeComponent();
        _interactionHelper = new ChartInteractionHelper(() => DataContext as ILapChartViewModel);

        List<AvaPlot> charts = [GasChart.Chart, BrakeChart.Chart, SteeringChart.Chart, SpeedChart.Chart, GearChart.Chart, RpmChart.Chart];
        _interactionHelper.AttachEvents(charts);
    }
}
