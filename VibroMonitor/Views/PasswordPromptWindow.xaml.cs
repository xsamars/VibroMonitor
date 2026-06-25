using System.Windows;

namespace VibroMonitor.Views
{
    public partial class PasswordPromptWindow : Window
    {
        public string? Password { get; private set; }
        public PasswordPromptWindow()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Password = Pwd.Password;
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
