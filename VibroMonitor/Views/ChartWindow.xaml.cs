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
                    if (labelerProp != null)
                        labelerProp.SetValue(axis, vm.XLabeler);
                    if (unitProp != null)
                        unitProp.SetValue(axis, 1);

                    var axesArray = Array.CreateInstance(axisType, 1);
                    axesArray.SetValue(axis, 0);

                    var chartType = Chart.GetType();
                    var xAxesProp = chartType.GetProperty("XAxes");
                    if (xAxesProp != null)
                        xAxesProp.SetValue(Chart, axesArray);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to setup chart axis: {ex.Message}");
                }
            }
        }
    }
}
