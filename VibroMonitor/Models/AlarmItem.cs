using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using System.ComponentModel.DataAnnotations;

namespace VibroMonitor.Models;

public partial class AlarmItem : ObservableObject
{
    [Key]
    public int Id { get; set; }
    [ObservableProperty]
    private string message;

    [ObservableProperty]
    private string status;

    [ObservableProperty]
    private DateTime created;

    [ObservableProperty]
    private DateTime ackedTime;

    [ObservableProperty]
    private string sensor;

    // Link to equipment point that produced this alarm (optional)
    public int? EquipmentPointId { get; set; }
    public EquipmentPoint? EquipmentPoint { get; set; }

    // Level of alert (Normal/Warning/Alarm)
    private AlertLevel level;
    public AlertLevel Level
    {
        get => level;
        set
        {
            if (!global::System.Collections.Generic.EqualityComparer<AlertLevel>.Default.Equals(level, value))
            {
                OnPropertyChanging(nameof(Level));
                level = value;
                OnPropertyChanged(nameof(Level));
            }
        }
    }

    private bool acked;

    public bool Acked
    {
        get => acked;
        set
        {
            if (!global::System.Collections.Generic.EqualityComparer<bool>.Default.Equals(acked, value))
            {
                OnPropertyChanging(nameof(Acked));
                acked = value;
                OnPropertyChanged(nameof(Acked));
                OnPropertyChanged(nameof(ShowAckButton));
                OnPropertyChanged(nameof(Created));
                OnPropertyChanged(nameof(CreatedLocal));

            }
        }
    }

    // Show ack button when alarm is NOT acked
    public Visibility ShowAckButton => Acked ? Visibility.Collapsed
            : Visibility.Visible;

    // Localized view of Created for UI display
    public DateTime CreatedLocal => Created.ToLocalTime();

    // Called by the source-generated setter when Created changes
    partial void OnCreatedChanged(DateTime value)
    {
        OnPropertyChanged(nameof(CreatedLocal));
    }

    // Localized view of AckedTime for UI display
    public DateTime AckedTimeLocal => AckedTime == default ? AckedTime : AckedTime.ToLocalTime();

    // Called when AckedTime changes
    partial void OnAckedTimeChanged(DateTime value)
    {
        OnPropertyChanged(nameof(AckedTimeLocal));
        OnPropertyChanged(nameof(AckedTimeDisplay));
    }

    // Display string for AckedTime: empty when not set
    public string AckedTimeDisplay => AckedTime == default ? string.Empty : AckedTime.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss");

    partial void OnStatusChanged(string value)
    {
        OnPropertyChanged(nameof(ShowAckButton));
    }
}
