using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using VibroMonitor.Services;
using VibroMonitor.Models;
using VibroMonitor.Views;
using VibroMonitor.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace VibroMonitor.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly AppDbContext _db;
        private readonly MqttService _mqttService;

        [ObservableProperty]
        private ObservableCollection<EquipmentItem> equipmentItems = new();

        [ObservableProperty]
        private ObservableCollection<AlarmItem> alarmItems = new();

        public MainViewModel(AppDbContext db, MqttService mqttService)
        {
            _db = db;
            _mqttService = mqttService;
        }

        private void OnMessageReceived(string topic, string payload)
        {
            // find point by topic
            foreach (var equipment in EquipmentItems)
            {
                var point = equipment.Points.FirstOrDefault(p => p.MqttTopic == topic);
                if (point != null)
                {
                    payload = payload.Replace('.', ',');
                    if (double.TryParse(payload, out var val))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            point.Value = val;

                            // update equipment alert
                            var prev = equipment.CurrentAlert;
                            equipment.UpdateAlertFromPoints();
                            if (equipment.CurrentAlert != prev)
                            {
                                if (equipment.CurrentAlert == Models.AlertLevel.Alarm)
                                {
                                    AlarmItems.Add(new AlarmItem { Message = $"Авария в {equipment.Name}", Status = "Активно", Created = DateTime.Now.ToString(), Sensor = point.Name });
                                }
                            }
                        });
                    }
                    break;
                }
            }
        }

        public async Task InitializeAsync()
        {
            // load equipment and their points from DB
            var items = await _db.EquipmentItems.Include(x => x.Points).ToListAsync();
            EquipmentItems.Clear();
            foreach (var it in items)
                EquipmentItems.Add(it);

            // subscribe to MQTT for all points
            await _mqttService.Connect();
            foreach (var it in EquipmentItems)
            {
                foreach (var p in it.Points)
                {
                    if (!string.IsNullOrWhiteSpace(p.MqttTopic))
                        await _mqttService.Subscribe(p.MqttTopic);
                }
            }

            _mqttService.MessageReceived += OnMessageReceived;

            // start blinking timer
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(600);
            timer.Tick += (s, e) =>
            {
                foreach (var it in EquipmentItems)
                {
                    if (it.CurrentAlert != Models.AlertLevel.Normal)
                        it.IsBlinkOn = !it.IsBlinkOn;
                    else
                        it.IsBlinkOn = false;
                }
            };
            timer.Start();

            AlarmItems.Add(new()
            {
                Message = "Превышение виброскорости",
                Status = "Активно",
                Created = "27.05.2026 15:26",
                Sensor = "Установка 1"
            }
                );
            // example static alarms (can be loaded from DB later)

        }

        [RelayCommand]
        private void OpenSettings()
        {
            MessageBox.Show("Настройки");
        }

        [RelayCommand]
        private void AckAlarm(AlarmItem item)
        {
            item.Status = "Квитировано";
        }
        [RelayCommand]
        private async Task OpenEquipment(EquipmentItem item)
        {
            await _mqttService.Connect();

            // ensure points are loaded
            var equipment = await _db.EquipmentItems.Include(x => x.Points).FirstOrDefaultAsync(x => x.Id == item.Id);
            if (equipment == null)
                return;

            var vm = new EquipmentDetailsViewModel(equipment, _mqttService, _db);

            var window = new EquipmentDetailsWindow()
            {
                DataContext = vm
            };

            window.ShowDialog();
        }

        [RelayCommand]
        private async Task AddEquipment()
        {
            var newItem = new EquipmentItem() { Name = "Новое оборудование" };
            _db.EquipmentItems.Add(newItem);
            await _db.SaveChangesAsync();
            EquipmentItems.Add(newItem);
        }

        [RelayCommand]
        private async Task RemoveEquipment(EquipmentItem item)
        {
            _db.EquipmentItems.Remove(item);
            await _db.SaveChangesAsync();
            EquipmentItems.Remove(item);
        }
    }
}
