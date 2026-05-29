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
    private string created;

    [ObservableProperty]
    private string acknowledged;

    [ObservableProperty]
    private string sensor;

    public Visibility ShowAckButton =>
        Status == "Активно"
            ? Visibility.Visible
            : Visibility.Collapsed;

    partial void OnStatusChanged(string value)
    {
        OnPropertyChanged(nameof(ShowAckButton));
    }
}
