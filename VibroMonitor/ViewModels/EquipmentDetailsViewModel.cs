using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Linq;
using VibroMonitor.Services;
using VibroMonitor.Models;
using VibroMonitor.Views;
using VibroMonitor.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Microsoft.Win32;
using System.IO;
using System.Threading;

namespace VibroMonitor.ViewModels;

public partial class EquipmentDetailsViewModel : ObservableObject, IDisposable
{
    private readonly MqttService _mqttService;
    private readonly AppDbContext _db;
    private readonly VibroMonitor.Services.AdminService _adminService;
    private CancellationTokenSource? _refreshCancellation;

    public EquipmentItem Equipment { get; }

    [ObservableProperty]
    private bool isEditMode;

    [ObservableProperty]
    private EquipmentPoint? selectedPoint;

    public EquipmentDetailsViewModel(EquipmentItem equipment, MqttService mqttService, AppDbContext db, VibroMonitor.Services.AdminService adminService)
    {
        Equipment = equipment;
        _mqttService = mqttService;
        _db = db;
        _adminService = adminService;

        _mqttService.MessageReceived += OnMessageReceived;

        // ensure points loaded from db
        _ = InitializeAsync();
        _adminService.AuthChanged += (ok) => OnPropertyChanged(nameof(IsAdminAuthenticated));
    }

    public bool IsAdminAuthenticated => _adminService?.IsAuthenticated ?? false;

    private async Task InitializeAsync()
    {
        await SubscribePointsAsync();
        StartAlarmRefreshLoop();
    }

    private void StartAlarmRefreshLoop()
    {
        _refreshCancellation = new CancellationTokenSource();
        _ = RefreshAlarmsLoop(_refreshCancellation.Token);
    }

    private async Task RefreshAlarmsLoop(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    break;

                await RefreshAlarmsFromDatabase();
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation, no action needed
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in alarm refresh loop: {ex.Message}");
        }
    }

    private async Task RefreshAlarmsFromDatabase()
    {
        try
        {
            var alarms = await _db.AlarmItem
                .Where(a => a.EquipmentPointId.HasValue && 
                            Equipment.Points.Select(p => p.Id).Contains(a.EquipmentPointId.Value))
                .OrderByDescending(a => a.Created)
                .Take(100)
                .ToListAsync();

            Application.Current.Dispatcher.Invoke(() =>
            {
                Equipment.Alarms.Clear();
                foreach (var alarm in alarms)
                {
                    Equipment.Alarms.Add(alarm);
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error refreshing alarms: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _refreshCancellation?.Cancel();
        _refreshCancellation?.Dispose();
        _mqttService.MessageReceived -= OnMessageReceived;
        _adminService.AuthChanged -= (ok) => OnPropertyChanged(nameof(IsAdminAuthenticated));
        GC.SuppressFinalize(this);
    }

    private async Task SubscribePointsAsync()
    {
        foreach (var point in Equipment.Points)
        {
            if (!string.IsNullOrWhiteSpace(point.MqttTopic))
            {
                await _mqttService.Subscribe(point.MqttTopic);
            }
        }
    }

    private void OnMessageReceived(string topic, string payload)
    {
        var point = Equipment.Points
            .FirstOrDefault(x => x.MqttTopic == topic);

        if (point == null)
            return;
        payload = payload.Replace(".", ",");
        if (!double.TryParse(payload, out var value))
            return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            point.Value = value;
        });
    }

    [RelayCommand]
    private async Task AckAlarm(AlarmItem item)
    {
        if (item == null) return;
        try
        {
            item.Status = "Квитировано";
            item.Acked = true;
            item.AckedTime = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при квитировании тревоги: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public async Task AddPoint(double x, double y)
    {
        if (!IsEditMode)
            return;

        if (!_adminService.IsAuthenticated)
        {
            var ok = _adminService.EnsureAuthenticated(Application.Current.MainWindow);
            if (!ok) return;
        }

        var pt = new EquipmentPoint()
        {
            Name = $"Точка {Equipment.Points.Count + 1}",
            X = x - 7,
            Y = y - 7,
            Unit = "мм/с",
            Value = 0
        };


        Equipment.Points.Add(pt);
        _db.EquipmentPoints.Add(pt);
        // open editor dialog(synchronous) to allow user to adjust point
        await EditPoint(pt);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при добавлении точки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ToggleEditMode()
    {
        // require admin to enable edit mode
        if (!IsEditMode)
        {
            if (!_adminService.IsAuthenticated)
            {
                var ok = _adminService.EnsureAuthenticated(Application.Current.MainWindow);
                if (!ok) return;
            }
        }
        IsEditMode = !IsEditMode;
    }

    [RelayCommand]
    private async Task RemovePoint(EquipmentPoint point)
    {
        if (!_adminService.IsAuthenticated)
        {
            var ok = _adminService.EnsureAuthenticated(Application.Current.MainWindow);
            if (!ok) return;
        }

        Equipment.Points.Remove(point);
        _db.EquipmentPoints.Remove(point);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при удалении точки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task EditPoint(EquipmentPoint point)
    {
        var vm = new EditPointViewModel(point, _db);

        var window = new EditPointWindow()
        {
            DataContext = vm
        };

       window.ShowDialog();

        try
        {
            await SubscribePointsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при подписке на точки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    [RelayCommand]
    private async Task ExportHistory()
    {
        try
        {
            var dlg = new ExportHistoryWindow();
            dlg.Owner = Application.Current.MainWindow;
            if (dlg.ShowDialog() != true)
                return;

            // collect parameters
            var fromLocal = dlg.From; // local time
            var toLocal = dlg.To;
            var interval = dlg.Interval; // TimeSpan

            if (fromLocal >= toLocal)
            {
                MessageBox.Show("Неверный диапазон дат.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var fromUtc = fromLocal.ToUniversalTime();
            var toUtc = toLocal.ToUniversalTime();

            // fetch history for all points of this equipment
            var pointIds = Equipment.Points.Select(p => p.Id).ToArray();

            var histories = await _db.PointHistory
                .Where(h => pointIds.Contains(h.EquipmentPointId) && h.Time >= fromUtc && h.Time <= toUtc)
                .OrderBy(h => h.Time)
                .ToListAsync();

            if (histories.Count == 0)
            {
                MessageBox.Show("Нет данных за выбранный период.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // create buckets
            var buckets = new SortedDictionary<long, DateTime>();
            var bucketData = new Dictionary<long, Dictionary<int, List<double>>>();

            foreach (var h in histories)
            {
                var bucketIndex = (long)Math.Floor((h.Time - fromUtc).TotalSeconds / interval.TotalSeconds);
                if (!buckets.ContainsKey(bucketIndex))
                    buckets[bucketIndex] = fromUtc.AddSeconds(bucketIndex * interval.TotalSeconds);
                if (!bucketData.ContainsKey(bucketIndex))
                    bucketData[bucketIndex] = new Dictionary<int, List<double>>();
                if (!bucketData[bucketIndex].ContainsKey(h.EquipmentPointId))
                    bucketData[bucketIndex][h.EquipmentPointId] = new List<double>();
                bucketData[bucketIndex][h.EquipmentPointId].Add(h.Value);
            }

            // prepare CSV
            var pointList = Equipment.Points.OrderBy(p => p.Id).ToList();

            var saveDlg = new SaveFileDialog()
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                FileName = $"History_{Equipment.Name}_{DateTime.Now:yyyyMMddHHmmss}.csv"
            };

            if (saveDlg.ShowDialog() != true)
                return;

            using var sw = new StreamWriter(saveDlg.FileName, false, System.Text.Encoding.UTF8);

            // header
            sw.Write("Time");
            foreach (var p in pointList)
            {
                sw.Write($";{p.Name}");
            }
            sw.WriteLine();

            // rows
            foreach (var kv in buckets)
            {
                var time = kv.Value.ToLocalTime();
                sw.Write(time.ToString("yyyy-MM-dd HH:mm:ss"));
                foreach (var p in pointList)
                {
                    if (bucketData.TryGetValue(kv.Key, out var dict) && dict.TryGetValue(p.Id, out var vals) && vals.Count > 0)
                    {
                        var avg = vals.Average();
                        sw.Write($";{avg}");
                    }
                    else
                    {
                        sw.Write(";");
                    }
                }
                sw.WriteLine();
            }

            MessageBox.Show($"Данные успешно экспортированы в {saveDlg.FileName}", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task OpenHighChart(EquipmentPoint point)
    {
        if (point == null) return;
        try
        {
            // load last 24 hours of data (UTC)
            var from = DateTime.UtcNow.AddDays(-360);
            var history = await _db.PointHistory
                .Where(h => h.EquipmentPointId == point.Id && h.Time >= from)
                .OrderBy(h => h.Time)
                .ToListAsync();

            var data = history
                .Select(h => (time: (long)(h.Time.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalMilliseconds, value: h.Value))
                .ToList();

            var window = new Views.HighChartWindow(point.Name, data, point.Hi == 0 ? (double?)null : point.Hi, point.HiHi == 0 ? (double?)null : point.HiHi);
            window.Owner = Application.Current.MainWindow;
            window.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось открыть HighCharts-график: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}