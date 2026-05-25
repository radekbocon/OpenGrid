using System.Collections.ObjectModel;

namespace SimLab.ViewModels;

public interface ILapChartViewModel
{
    ObservableCollection<ChartData> Inputs { get; }
    ObservableCollection<ChartData> Steering { get; }
    ObservableCollection<ChartData> Speed { get; }
    ObservableCollection<ChartData> Gear { get; }
    ObservableCollection<ChartData> Rpm { get; }
    
    double DataMinX { get; }
    double DataMaxX { get; }
    double MinX { get; set; }
    double MaxX { get; set; }
}
