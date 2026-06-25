using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VibroMonitor.ViewModels;

namespace VibroMonitor.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;

        // initialize async data loading
        _ = vm.InitializeAsync();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var admin = App.Services?.GetService(typeof(VibroMonitor.Services.AdminService)) as VibroMonitor.Services.AdminService;
        if (admin != null && admin.IsAuthenticated)
        {
            SettingsBtn.Visibility = Visibility.Visible; // Show settings button if authenticated
        }
        else
        {
            SettingsBtn.Visibility = Visibility.Collapsed; // Hide settings button if not authenticated
        }
    }

    private void OnEquipmentClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Determine the data context of the clicked item
        if (sender is FrameworkElement fe && fe.DataContext is VibroMonitor.Models.EquipmentItem item)
        {
            if (DataContext is VibroMonitor.ViewModels.MainViewModel vm)
            {
                _ = vm.OpenEquipmentCommand.ExecuteAsync(item);
            }
        }
    }
}
