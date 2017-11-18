using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MailClient
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string _userDataPath;
        public string UserDataPath => _userDataPath;

        private readonly string _directoryUserDataPath;
        public string DirectoryUserDataPath => _directoryUserDataPath;

        public User CurrentUser { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            this._directoryUserDataPath = @"C:\Users\" + Environment.UserName + @"\MailClient\";
            this._userDataPath = DirectoryUserDataPath + Environment.UserName + "_userdata.mcd";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(DirectoryUserDataPath))
            {
                Directory.CreateDirectory(DirectoryUserDataPath);
            }

            if (File.Exists(this.UserDataPath))
            {
                this.CurrentUser = BinarySerializer.Deserialize<User>(this.UserDataPath);
            }
            else
            {
                AuthWindow authWindow = new AuthWindow() { Owner = this };
                authWindow.ShowDialog();
                this.CurrentUser = authWindow.AuthUser;
            }

            if (this.CurrentUser is null)
                this.Close();
            // TODO: обработка, если юзер зашёл
        }
    }
}
