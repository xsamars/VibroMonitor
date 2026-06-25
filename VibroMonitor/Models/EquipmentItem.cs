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

    // images navigation
    public ObservableCollection<EquipmentImage> Images { get; set; } = new();

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public byte[]? ImageData => Images?.FirstOrDefault()?.Data;

    // Placeholder for alarms
    public ObservableCollection<AlarmItem> Alarms { get; set; } = new();

    // Alerting state (observable so UI updates)
    [property: NotMapped]
    [ObservableProperty]
    private Models.AlertLevel currentAlert = Models.AlertLevel.Normal;

    [property: NotMapped]
    [ObservableProperty]
    private bool isBlinkOn;

    public void UpdateAlertFromPoints()
    {
        var highest = Models.AlertLevel.Normal;
        foreach (var p in Points)
        {
            var lvl = p.GetAlertLevel();
            if ((int)lvl > (int)highest)
                highest = lvl;
            if (highest == Models.AlertLevel.HiHi)
                break;
        }

        CurrentAlert = highest;
    }
}
