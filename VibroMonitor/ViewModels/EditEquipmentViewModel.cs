using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using VibroMonitor.Models;

namespace VibroMonitor.ViewModels
{
    public partial class EditEquipmentViewModel : ObservableObject
    {
        [ObservableProperty]
        private string name = "";

        [ObservableProperty]
        private string imagePath = "";

        private EquipmentItem? _equipment;
        private Action? _onSaved;

        public EditEquipmentViewModel() { }

        public void Initialize(EquipmentItem? equipment, Action? onSaved = null)
        {
            _equipment = equipment;
            _onSaved = onSaved;

            if (equipment != null)
            {
                this.Name = equipment.Name;
                this.ImagePath = equipment.ImagePath;
            }
            else
            {
                this.Name = "";
                this.ImagePath = "";
            }
        }

        [RelayCommand]
        private void Save(Window window)
        {
            if (string.IsNullOrWhiteSpace(this.Name))
            {
                MessageBox.Show("Пожалуйста, введите название оборудования", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_equipment != null)
            {
                _equipment.Name = this.Name;
                _equipment.ImagePath = this.ImagePath;
            }

            _onSaved?.Invoke();
            window.DialogResult = true;
            window.Close();
        }

        [RelayCommand]
        private void Cancel(Window window)
        {
            window.DialogResult = false;
            window.Close();
        }
    }
}
