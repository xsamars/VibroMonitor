using VibroMonitor.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VibroMonitor.ViewModels;

public partial class PointChartViewModel : ObservableObject
{
    public EquipmentPoint Point { get; }

    public PointChartViewModel(EquipmentPoint point)
    {
        Point = point;
    }
}
