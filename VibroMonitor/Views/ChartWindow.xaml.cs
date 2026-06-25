using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using LiveChartsCore.SkiaSharpView.WPF;
using LiveChartsCore.Measure;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace VibroMonitor.Views
{
    /// <summary>
    /// Логика взаимодействия для ChartWindow.xaml
    /// </summary>
    public partial class ChartWindow : Window
    {
        public ChartWindow()
        {
            InitializeComponent();
            Loaded += ChartWindow_Loaded;
        }

        private void ChartWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            if (DataContext is VibroMonitor.ViewModels.PointChartViewModel vm)
            {
                try
                {
                    // Find Axis type in loaded LiveCharts assemblies
                    var axisType = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => {
                            try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
                        })
                        .FirstOrDefault(t => t.Name == "Axis" && t.Namespace != null && t.Namespace.StartsWith("LiveChartsCore"));

                    if (axisType == null)
                        return;

                    var axis = Activator.CreateInstance(axisType);

                    var labelerProp = axisType.GetProperty("Labeler");
                    var unitProp = axisType.GetProperty("Unit");
                    var crossSnapProp = axisType.GetProperty("CrosshairSnapEnabled");
                    var crossLabelsBgProp = axisType.GetProperty("CrosshairLabelsBackground");
                    var crossLabelsPaintProp = axisType.GetProperty("CrosshairLabelsPaint");
                    var crossPaintProp = axisType.GetProperty("CrosshairPaint");

                    if (labelerProp != null)
                        labelerProp.SetValue(axis, vm.XLabeler);
                    if (unitProp != null)
                        unitProp.SetValue(axis, 1);

                    // If the view model updates XLabeler later, update the axis Labeler as well
                    if (labelerProp != null)
                    {
                        vm.PropertyChanged += (s, ev) =>
                        {
                            try
                            {
                                if (ev.PropertyName == nameof(vm.XLabeler))
                                {
                                    // update labeler on UI thread
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        try { labelerProp.SetValue(axis, vm.XLabeler); } catch { }
                                    });
                                }
                            }
                            catch { }
                        };
                    }
                    // enable crosshair snapping and set visuals if available on this Axis type
                    if (crossSnapProp != null)
                        crossSnapProp.SetValue(axis, true);
                    if (crossLabelsBgProp != null)
                        crossLabelsBgProp.SetValue(axis, "#FF8C00");
                    if (crossLabelsPaintProp != null)
                        crossLabelsPaintProp.SetValue(axis, "#8B0000");
                    if (crossPaintProp != null)
                        crossPaintProp.SetValue(axis, "#FF8C00");

                    // previously axes were created dynamically; XAML binding handles it now
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to setup chart axis: {ex.Message}");
                }
            }
        }
    }
}
