using System.Collections.Generic;
using SimLab.Controls;

namespace SimLab.ViewModels;

public interface ILapChartViewModel
{
    List<SubplotDefinition> Subplots { get; }
}
