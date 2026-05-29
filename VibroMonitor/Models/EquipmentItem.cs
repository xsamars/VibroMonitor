using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace VibroMonitor.Models;

public partial class EquipmentItem : ObservableObject
{
    [ObservableProperty]
    private string name = "";

    [ObservableProperty]
    private string imagePath = "";

    public ObservableCollection<EquipmentPoint> Points { get; set; } = [];

    public ObservableCollection<AlarmItem> Alarms { get; set; } = [];
}