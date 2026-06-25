using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using VibroMonitor.Models;
using Microsoft.Win32;
using System.IO;
using VibroMonitor.Data;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;

namespace VibroMonitor.ViewModels
{
    public partial class EditEquipmentViewModel : ObservableObject
    {
        [ObservableProperty]
        private string name = "";

        [ObservableProperty]
        private string imagePath = "";

        [ObservableProperty]
        private byte[]? selectedImageBytes;

        private EquipmentItem? _equipment;
        private Action? _onSaved;
    private readonly AppDbContext? _db;

    public EditEquipmentViewModel(AppDbContext? db = null)
    {
        _db = db;
    }

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

    private string? _selectedFilePath;

    [RelayCommand]
    private void ChooseImage()
    {
        var dlg = new OpenFileDialog();
        dlg.Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.webp";
        if (dlg.ShowDialog() == true)
        {
            ImagePath = dlg.FileName;
            _selectedFilePath = dlg.FileName;
            try
            {
                // create small preview thumbnail bytes
                using var img = Image.FromFile(_selectedFilePath);
                var targetW = 200;
                var ratio = (double)targetW / img.Width;
                var targetH = (int)(img.Height * ratio);
                using var thumb = new Bitmap(img, new System.Drawing.Size(targetW, targetH));
                using var ms = new MemoryStream();
                thumb.Save(ms, ImageFormat.Jpeg);
                selectedImageBytes = ms.ToArray();
            }
            catch { selectedImageBytes = null; }
        }
    }

        [RelayCommand]
        private async Task Save(Window window)
        {
            if (string.IsNullOrWhiteSpace(this.Name))
            {
                MessageBox.Show("Пожалуйста, введите название оборудования", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_equipment != null)
            {
                _equipment.Name = this.Name;
                if (_db != null)
                {
                    // ensure equipment persisted to get Id
                    if (_equipment.Id == 0)
                    {
                        _db.EquipmentItems.Add(_equipment);
                        await _db.SaveChangesAsync();
                    }

                    // if a file was selected, save images
                    if (!string.IsNullOrWhiteSpace(_selectedFilePath) && File.Exists(_selectedFilePath))
                    {
                        try
                        {
                            var bytes = File.ReadAllBytes(_selectedFilePath);

                var existingImages = _db.EquipmentImages.Where(i => i.EquipmentItemId == _equipment.Id).ToList();
                foreach (var ex in existingImages)
                    _db.EquipmentImages.Remove(ex);

                var imgRec = new EquipmentImage
                {
                    EquipmentItemId = _equipment.Id,
                    Data = bytes,
                    MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg,
                    FileName = Path.GetFileName(_selectedFilePath),
                    IsThumbnail = false,
                    CreatedAt = DateTime.UtcNow
                };

                _db.EquipmentImages.Add(imgRec);
                await _db.SaveChangesAsync();

                // update in-memory navigation so UI can show image immediately
                try
                {
                    // clear existing images in-memory
                    _equipment.Images.Clear();
                    _equipment.Images.Add(imgRec);
                }
                catch { }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка при сохранении изображения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
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
