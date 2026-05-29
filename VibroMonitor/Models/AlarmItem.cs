using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;

namespace VibroMonitor.Models;

public partial class AlarmItem : ObservableObject
{
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
