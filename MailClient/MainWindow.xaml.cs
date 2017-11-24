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
        public User CurrentUser { get; set; }
        public OptionsWindow OptionsWindow { get; set; }
        public static readonly string UserDirectoryPath = @"C:\Users\" + Environment.UserName + @"\MailClient\";
        public static readonly string UserDataPath = MainWindow.UserDirectoryPath + Environment.UserName + "_userdata.mcd";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(MainWindow.UserDirectoryPath))
            {
                Directory.CreateDirectory(MainWindow.UserDirectoryPath);
            }

            if (File.Exists(MainWindow.UserDataPath))
            {
                byte[] data = Encrypter.AesDecryptFile(MainWindow.UserDataPath,
                    Encrypter.DefaultKey, Encrypter.DefaultIV);
                this.CurrentUser = BinarySerializer.Deserialize<User>(data);
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

        private void OptionsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (OptionsWindow is null)
            {
                this.OptionsWindow = new OptionsWindow() { Owner = this };
                this.OptionsWindow.ShowDialog();
            }
        }
    }
}
