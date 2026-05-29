using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using VibroMonitor.Data;
using VibroMonitor.Models;

namespace VibroMonitor.ViewModels;

public partial class EditPointViewModel : ObservableObject
{
    public EquipmentPoint Point { get; }

    private AppDbContext _context;

    public EditPointViewModel(EquipmentPoint point, AppDbContext context)
    {
        Point = point;
        _context = context;
    }

    [RelayCommand]
    private void Save()
    {
        _context.SaveChangesAsync();
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