using System;
using System.Windows;
using System.Windows.Controls;

namespace VibroMonitor.Views
{
    public partial class ExportHistoryWindow : Window
    {
        public DateTime From { get; private set; }
        public DateTime To { get; private set; }
        public TimeSpan Interval { get; private set; }

        public ExportHistoryWindow()
        {
            InitializeComponent();
            cbInterval.SelectedIndex = 0;

            // set default dates
            dpFromDate.SelectedDate = DateTime.Now.Date;
            dpToDate.SelectedDate = DateTime.Now.Date;
            tbFromTime.Text = "00:00";
            tbToTime.Text = DateTime.Now.ToString("HH:mm");
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!dpFromDate.SelectedDate.HasValue || !dpToDate.SelectedDate.HasValue)
                {
                    MessageBox.Show("Выберите даты.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var fromDate = dpFromDate.SelectedDate.Value;
                var toDate = dpToDate.SelectedDate.Value;

                if (!TimeSpan.TryParse(tbFromTime.Text, out var fromTime))
                {
                    MessageBox.Show("Неверный формат времени (С).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!TimeSpan.TryParse(tbToTime.Text, out var toTime))
                {
                    MessageBox.Show("Неверный формат времени (По).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var sel = cbInterval.SelectedItem as ComboBoxItem;
                if (sel == null || !TimeSpan.TryParse((string)sel.Tag, out var interval))
                {
                    MessageBox.Show("Выберите интервал.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                From = fromDate.Date + fromTime;
                To = toDate.Date + toTime;
                Interval = interval;

                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
