using System;
using System.Collections.ObjectModel;
using SimLab.ViewModels;

namespace SimLab.Controls;

public class SubplotDefinition
{
    public string Title { get; set; } = string.Empty;
    public ObservableCollection<ChartData> Series { get; set; } = [];
    public Func<double, string>? YLabeler { get; set; }
    public double? YMinLimit { get; set; }
    public double? YMaxLimit { get; set; }
}
