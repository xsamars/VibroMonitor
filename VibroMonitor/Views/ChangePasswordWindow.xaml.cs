using System.Windows;
using VibroMonitor.Services;

namespace VibroMonitor.Views
{
    public partial class ChangePasswordWindow : Window
    {
        private readonly AdminService _admin;
        public ChangePasswordWindow(AdminService admin)
        {
            _admin = admin;
            InitializeComponent();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (NewPwd.Password != ConfirmPwd.Password)
            {
                MessageBox.Show("Пароли не совпадают", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            _admin.ChangePassword(NewPwd.Password);
            MessageBox.Show("Пароль изменён", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
