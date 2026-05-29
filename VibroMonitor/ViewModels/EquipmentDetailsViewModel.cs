using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using Vibromonitor.Services;
using Vibromonitor.ViewModels;
using VibroMonitor.Models;
using VibroMonitor.Views;

namespace VibroMonitor.ViewModels;

public partial class EquipmentDetailsViewModel : ObservableObject
{
    private readonly MqttService _mqttService;
    public EquipmentItem Equipment { get; }

    [ObservableProperty]
    private bool isEditMode;

    [ObservableProperty]
    private EquipmentPoint? selectedPoint;

    public EquipmentDetailsViewModel(EquipmentItem equipment, MqttService mqttService)
    {
        Equipment = equipment;
        _mqttService = mqttService;

        _mqttService.MessageReceived += OnMessageReceived;

        SubscribePoints();
    }

    private async void SubscribePoints()
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

            point.History.Add(new PointValue()
            {
                Time = DateTime.Now,
                Value = value
            });

            // Ограничение истории
            while (point.History.Count > 500)
            {
                point.History.RemoveAt(0);
            }
        });
    }

    public void AddPoint(double x, double y)
    {
        if (!IsEditMode)
            return;

        Equipment.Points.Add(new EquipmentPoint()
        {
            Name = $"Точка {Equipment.Points.Count + 1}",
            X = x - 7,
            Y = y - 7,
            Unit = "мм/с",
            Value = 0
        });
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
    }

    [RelayCommand]
    private void EditPoint(EquipmentPoint point)
    {
        var vm = new EditPointViewModel(point);

        var window = new EditPointWindow()
        {
            DataContext = vm
        };

        window.ShowDialog();

        SubscribePoints();
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