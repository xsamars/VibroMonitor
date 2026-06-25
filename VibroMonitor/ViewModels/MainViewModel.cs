using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Linq;
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
        private readonly AdminService _adminService;

        [ObservableProperty]
        private ObservableCollection<EquipmentItem> equipmentItems = new();

        [ObservableProperty]
        private ObservableCollection<AlarmItem> alarmItems = new();

        public MainViewModel(AppDbContext db, MqttService mqttService, AdminService adminService)
        {
            _db = db;
            _mqttService = mqttService;
            _adminService = adminService;
            _mqttService.MessageReceived += OnMessageReceived;
            // show settings button when admin authenticated
            _adminService.AuthChanged += (ok) => OnPropertyChanged(nameof(IsAdminAuthenticated));
        }

        // Expose admin authentication state to the view for bindings
        public bool IsAdminAuthenticated => _adminService?.IsAuthenticated ?? false;

        // timer to refresh alarms periodically
        private System.Windows.Threading.DispatcherTimer? _alarmsTimer;

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
                            var levelBefore = prev;
                            equipment.UpdateAlertFromPoints();
                            var levelAfter = equipment.CurrentAlert;

                            Console.WriteLine($"[DEBUG] MQTT topic={topic} point={point.Name} value={val} computedLevel={point.GetAlertLevel()} equipmentPrev={levelBefore} equipmentNow={levelAfter}");

                            if (equipment.CurrentAlert != prev)
                            {
                                if (equipment.CurrentAlert == Models.AlertLevel.HiHi || equipment.CurrentAlert == Models.AlertLevel.LoLo)
                                {
                                    equipment.IsBlinkOn = true;
                                }
                                else
                                {
                                    equipment.IsBlinkOn = false;
                                }
                                Console.WriteLine($"[DEBUG] Equipment '{equipment.Name}' CurrentAlert changed {levelBefore} -> {equipment.CurrentAlert}, IsBlinkOn={equipment.IsBlinkOn}");
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
            var items = await _db.EquipmentItems.Include(x => x.Points).Include(x => x.Images).ToListAsync();
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

            // start alarms refresh timer (every 5 seconds)
            _alarmsTimer = new System.Windows.Threading.DispatcherTimer();
            _alarmsTimer.Interval = TimeSpan.FromSeconds(5);
            _alarmsTimer.Tick += async (s, e) => await RefreshAlarmsAsync();
            _alarmsTimer.Start();

            // initial load of alarms
            await RefreshAlarmsAsync();

        }

        // OpenSettings is implemented below (kept single implementation)
        [RelayCommand]
        private void OpenSettings()
        {
            // require admin
            if (!_adminService.IsAuthenticated)
            {
                var pw = App.Services?.GetService(typeof(PasswordPromptWindow)) as Views.PasswordPromptWindow;
                if (pw == null) return;
                pw.Owner = Application.Current.MainWindow;
                var res = pw.ShowDialog();
                if (res != true) return;
                if (!_adminService.CheckPassword(pw.Password ?? string.Empty))
                {
                    MessageBox.Show("Неверный пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else
            {
                var pwchange = App.Services?.GetService(typeof(ChangePasswordWindow)) as Views.ChangePasswordWindow;
                pwchange.Owner = Application.Current.MainWindow;
                if (pwchange.ShowDialog() ?? true)
                    _db.SaveChangesAsync();

            }
        }

        [RelayCommand]
        private async Task AckAlarm(AlarmItem item)
        {
            item.Status = "Квитировано";
            item.Acked = true;
            item.AckedTime = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        private async Task RefreshAlarmsAsync()
        {
            try
            {
                var alarms = await _db.AlarmItem
                    .OrderByDescending(a => a.Created)
                    .ToListAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    AlarmItems.Clear();
                    foreach (var a in alarms)
                        AlarmItems.Add(a);
                });
            }
            catch (Exception ex)
            {
                // log or ignore for now
                Console.WriteLine($"Failed to refresh alarms: {ex.Message}");
            }
        }
        [RelayCommand]
        private async Task OpenEquipment(EquipmentItem item)
        {
            await _mqttService.Connect();

            // ensure points are loaded
            var equipment = await _db.EquipmentItems.Include(x => x.Points).FirstOrDefaultAsync(x => x.Id == item.Id);
            if (equipment == null)
                return;

            var admin = App.Services?.GetService(typeof(AdminService)) as AdminService;
            var vm = new EquipmentDetailsViewModel(equipment, _mqttService, _db, admin!);

            var window = new EquipmentDetailsWindow()
            {
                DataContext = vm
            };

            window.ShowDialog();
        }

        [RelayCommand]
        private async Task AddEquipment()
        {
            var newItem = new EquipmentItem() { Name = "", ImagePath = "" };

            var vm = new EditEquipmentViewModel(_db);
            vm.Initialize(newItem, () =>
            {
                // VM saved the entity to DB; just add to UI collection
                EquipmentItems.Add(newItem);
            });

            var window = new EditEquipmentWindow()
            {
                DataContext = vm
            };

            var result = window.ShowDialog();
            if (result != true)
            {
                // Если отменили, удалим из БД если что-то там было
                if (newItem.Id != 0)
                {
                    _db.EquipmentItems.Remove(newItem);
                    await _db.SaveChangesAsync();
                }
            }
        }

        [RelayCommand]
        private async Task EditEquipment(EquipmentItem item)
        {
            var vm = new EditEquipmentViewModel(_db);
            vm.Initialize(item, () =>
            {
                // persistence handled in VM
            });

            var window = new EditEquipmentWindow()
            {
                DataContext = vm
            };

            window.ShowDialog();
            var items = await _db.EquipmentItems.Include(x => x.Points).Include(x => x.Images).ToListAsync();
            EquipmentItems.Clear();
            foreach (var it in items)
                EquipmentItems.Add(it);
        }

        [RelayCommand]
        private async Task RemoveEquipment(EquipmentItem item)
        {
            var result = MessageBox.Show($"Вы уверены, что хотите удалить \"{item.Name}\"?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            _db.EquipmentItems.Remove(item);
            await _db.SaveChangesAsync();
            EquipmentItems.Remove(item);
        }
    }
}
