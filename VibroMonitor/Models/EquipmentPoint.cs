using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VibroMonitor.Models;

public partial class EquipmentPoint : ObservableObject
{
    [Key]
    public int Id { get; set; }

    // Foreign key to EquipmentItem
    public int EquipmentItemId { get; set; }

    // Navigation to parent equipment
    public EquipmentItem? EquipmentItem { get; set; }

    [ObservableProperty]
    private string name = "";

    [ObservableProperty]
    private double x;

    [ObservableProperty]
    private double y;

    [ObservableProperty]
    private double value;

    // Thresholds
    [ObservableProperty]
    private double hiHi;

    [ObservableProperty]
    private double hi;

    [ObservableProperty]
    private double lo;

    [ObservableProperty]
    private double loLo;

    [ObservableProperty]
    private string unit = "";

    // MQTT topic
    [ObservableProperty]
    private string mqttTopic = "";

    // Например:
    // vibration/motor1/bearing2

    public Models.AlertLevel GetAlertLevel()
    {
        // evaluate based on Value
        if (Value >= HiHi) return Models.AlertLevel.Alarm;
        if (Value >= Hi) return Models.AlertLevel.Warning;
        if (Value <= LoLo) return Models.AlertLevel.Alarm;
        if (Value <= Lo) return Models.AlertLevel.Warning;
        return Models.AlertLevel.Normal;
    }
}