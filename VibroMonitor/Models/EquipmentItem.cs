using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VibroMonitor.Models;

public partial class EquipmentItem : ObservableObject
{
    public int Id { get; set; }

    [ObservableProperty]
    private string name = "";

    [ObservableProperty]
    private string imagePath = "";

    // EF navigation
    public ObservableCollection<EquipmentPoint> Points { get; set; } = new();

    // Placeholder for alarms
    public ObservableCollection<AlarmItem> Alarms { get; set; } = new();

    // Alerting state
    [NotMapped]
    public Models.AlertLevel CurrentAlert { get; set; } = Models.AlertLevel.Normal;

    [NotMapped]
    public bool IsBlinkOn { get; set; }

    public void UpdateAlertFromPoints()
    {
        var highest = Models.AlertLevel.Normal;
        foreach (var p in Points)
        {
            var lvl = p.GetAlertLevel();
            if ((int)lvl > (int)highest)
                highest = lvl;
            if (highest == Models.AlertLevel.Alarm)
                break;
        }

        CurrentAlert = highest;
    }
}
