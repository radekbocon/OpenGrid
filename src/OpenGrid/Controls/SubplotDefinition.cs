using System;
using System.Collections.ObjectModel;
using OpenGrid.Models;
using OpenGrid.ViewModels;

namespace OpenGrid.Controls;

public class SubplotDefinition
{
    public string Title { get; set; } = string.Empty;
    public ObservableCollection<ChartData> Series { get; set; } = [];
    public Func<double, string>? YLabeler { get; set; }
    public double? YMinLimit { get; set; }
    public double? YMaxLimit { get; set; }
}
