using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using Vibromonitor.Services;
using VibroMonitor.Models;
using VibroMonitor.Views;

namespace VibroMonitor.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        public ObservableCollection<EquipmentItem> EquipmentItems { get; set; }

        public ObservableCollection<AlarmItem> AlarmItems { get; set; }

        public MainViewModel()
        {
            EquipmentItems = new ObservableCollection<EquipmentItem>()
        {
            new()
            {
                Name = "Привод конвейерной ленты",
                ImagePath = "../Images/1.jpg"
            },

            new()
            {
                Name = "Привод печи",
                ImagePath = "../Images/2.jpg"
            },

            new()
            {
                Name = "Двигатель основной",
                ImagePath = "../Images/3.jpg"
            },
            new()
            {
                Name = "Привод печи",
                ImagePath = "../Images/4.jpg"
            },
            new()
            {
                Name = "Привод печи",
                ImagePath = "../Images/5.jpg"
            },
            new()
            {
                Name = "Привод печи",
                ImagePath = "../Images/1.jpg"
            },
            new()
            {
                Name = "Привод печи",
                ImagePath = "../Images/2.jpg"
            },
            new()
            {
                Name = "Привод печи",
                ImagePath = "../Images/3.jpg"
            },
            new()
            {
                Name = "Привод печи",
                ImagePath = "../Images/4.jpg"
            },
            new()
            {
                Name = "Привод печи",
                ImagePath = "../Images/5.jpg"
            }
        };

            AlarmItems = new ObservableCollection<AlarmItem>()
        {
            new()
            {
                Message = "Превышение виброскорости",
                Status = "Активно",
                Created = "27.05.2026 15:26",
                Sensor = "Установка 1"
            },
            new()
            {
                Message = "Превышение виброскорости",
                Status = "Квитировано",
                Created = "27.05.2026 15:26",
                Sensor = "Установка 1"
            },
            new()
            {
                Message = "Превышение виброскорости",
                Status = "Активно",
                Created = "27.05.2026 15:26",
                Sensor = "Установка 1"
            }
        };
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
        MqttService _mqttService = new MqttService();
        [RelayCommand]
        private async Task OpenEquipment(EquipmentItem item)
        {
            await _mqttService.Connect();
            var vm = new EquipmentDetailsViewModel(item, _mqttService);

            var window = new EquipmentDetailsWindow()
            {
                DataContext = vm
            };

            window.ShowDialog();
        }
    }
}
