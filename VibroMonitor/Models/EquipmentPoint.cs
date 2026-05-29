using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace VibroMonitor.Models;

public partial class EquipmentPoint : ObservableObject
{
    [ObservableProperty]
    private string name = "";

    [ObservableProperty]
    private double x;

    [ObservableProperty]
    private double y;

    [ObservableProperty]
    private double value;

    [ObservableProperty]
    private string unit = "";

    // MQTT topic
    [ObservableProperty]
    private string mqttTopic = "";

    // Например:
    // vibration/motor1/bearing2


    // История для графика
    public ObservableCollection<PointValue> History { get; set; } = [];
}