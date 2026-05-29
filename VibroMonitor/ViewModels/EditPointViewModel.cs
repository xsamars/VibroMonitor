using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using VibroMonitor.Models;

namespace VibroMonitor.ViewModels;

public partial class EditPointViewModel : ObservableObject
{
    public EquipmentPoint Point { get; }

    public EditPointViewModel(EquipmentPoint point)
    {
        Point = point;
    }

    [RelayCommand]
    private void Save()
    {
        CloseWindow(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseWindow(false);
    }

    private void CloseWindow(bool result)
    {
        foreach (Window window in Application.Current.Windows)
        {
            if (window.DataContext == this)
            {
                window.DialogResult = result;
                window.Close();
                break;
            }
        }
    }
}