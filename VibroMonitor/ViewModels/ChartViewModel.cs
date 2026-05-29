using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using VibroMonitor.Models;

namespace Vibromonitor.ViewModels;

public partial class PointChartViewModel : ObservableObject
{
    public ISeries[] Series { get; set; }

    public PointChartViewModel(EquipmentPoint point)
    {
        Series =
        [
            new LineSeries<double>
            {
                Values = point.History
                    .Select(x => x.Value)
                    .ToArray()
            }
        ];
    }
}