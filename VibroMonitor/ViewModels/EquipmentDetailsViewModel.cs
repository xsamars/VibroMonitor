using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using VibroMonitor.Services;
using VibroMonitor.Models;
using VibroMonitor.Views;
using VibroMonitor.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace VibroMonitor.ViewModels;

public partial class EquipmentDetailsViewModel : ObservableObject
{
    private readonly MqttService _mqttService;
    private readonly AppDbContext _db;

    public EquipmentItem Equipment { get; }

    [ObservableProperty]
    private bool isEditMode;

    [ObservableProperty]
    private EquipmentPoint? selectedPoint;

    public EquipmentDetailsViewModel(EquipmentItem equipment, MqttService mqttService, AppDbContext db)
    {
        Equipment = equipment;
        _mqttService = mqttService;
        _db = db;

        _mqttService.MessageReceived += OnMessageReceived;

        // ensure points loaded from db
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var eq = await _db.EquipmentItems.Include(x => x.Points).FirstOrDefaultAsync(x => x.Id == Equipment.Id);
        if (eq != null)
        {
            Equipment.Points = eq.Points;
        }

        await SubscribePointsAsync();
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

    public void AddPoint(double x, double y)
    {
        if (!IsEditMode)
            return;

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
        _db.SaveChanges();
    }

    [RelayCommand]
    private void ToggleEditMode()
    {
        IsEditMode = !IsEditMode;
    }

    [RelayCommand]
    private void RemovePoint(EquipmentPoint point)
    {
        Equipment.Points.Remove(point);
        _db.EquipmentPoints.Remove(point);
        _ = _db.SaveChangesAsync();
    }

    [RelayCommand]
    private void EditPoint(EquipmentPoint point)
    {
        var vm = new EditPointViewModel(point, _db);

        var window = new EditPointWindow()
        {
            DataContext = vm
        };

       window.ShowDialog();

        _ = SubscribePointsAsync();
    }

    [RelayCommand]
    private void OpenChart(EquipmentPoint point)
    {
        var vm = new PointChartViewModel(point);

        var window = new ChartWindow()
        {
            DataContext = vm
        };

        window.Show();
    }
}