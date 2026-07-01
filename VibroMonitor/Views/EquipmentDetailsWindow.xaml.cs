using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VibroMonitor.ViewModels;

namespace VibroMonitor.Views
{
    /// <summary>
    /// Логика взаимодействия для EquipmentDetailsWindow.xaml
    /// </summary>
    public partial class EquipmentDetailsWindow : Window
    {
        public EquipmentDetailsWindow()
        {
            InitializeComponent();
            this.Closing += EquipmentDetailsWindow_Closing;
        }

        private void EquipmentDetailsWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not EquipmentDetailsViewModel vm)
                return;

            if (!vm.IsEditMode)
                return;

            var position = e.GetPosition((IInputElement)sender);

            vm.AddPoint(position.X, position.Y);
        }
    }
}
