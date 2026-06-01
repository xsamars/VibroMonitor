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
        // evaluate based on Value and return specific level for which threshold triggered
        if (Value >= HiHi) return Models.AlertLevel.HiHi;
        if (Value >= Hi) return Models.AlertLevel.Hi;
        if (Value <= LoLo) return Models.AlertLevel.LoLo;
        if (Value <= Lo) return Models.AlertLevel.Lo;
        return Models.AlertLevel.Normal;
    }

    // Computed level for UI binding (updates when Value changes)
    [System.Text.Json.Serialization.JsonIgnore]
    public Models.AlertLevel ComputedLevel => GetAlertLevel();

    partial void OnValueChanged(double value)
    {
        OnPropertyChanged(nameof(ComputedLevel));
    }
}