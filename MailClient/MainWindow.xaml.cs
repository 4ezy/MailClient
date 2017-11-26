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
        public static readonly string UserDirectoryPath = @"C:\Users\" + Environment.UserName + @"\MailClient\";
        public static readonly string RememberMeDataPath = MainWindow.UserDirectoryPath + "remdata.mcd";

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

            if (File.Exists(MainWindow.RememberMeDataPath))
            {
                byte[] rememberMeData = Encrypter.AesDecryptFile(MainWindow.RememberMeDataPath,
                    Encrypter.DefaultKey, Encrypter.DefaultIV);
                string rememberMeLogin = BinarySerializer.Deserialize<string>(rememberMeData);
                byte[] userData = Encrypter.AesDecryptFile(MainWindow.UserDirectoryPath + rememberMeLogin + ".mcd",
                    Encrypter.DefaultKey, Encrypter.DefaultIV);
                this.CurrentUser = BinarySerializer.Deserialize<User>(userData);
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
            // TODO: удалять старый файл
            AppOptionsWindow optionsWindow = new AppOptionsWindow()
            {
                Owner = this,
                User = (User)this.CurrentUser.Clone()
            };
            optionsWindow.ShowDialog();
            
            if (!this.CurrentUser.Equals(optionsWindow.User))
            {
                this.CurrentUser = optionsWindow.User;
                byte[] serData = BinarySerializer.Serialize(this.CurrentUser);
                byte[] encSerData = Encrypter.AesEncrypt(serData,
                    Encrypter.DefaultKey, Encrypter.DefaultIV);
                File.WriteAllBytes(MainWindow.UserDirectoryPath + this.CurrentUser.Login + ".mcd", encSerData);
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.CurrentUser = null;

            if (File.Exists(MainWindow.RememberMeDataPath))
                File.Delete(MainWindow.RememberMeDataPath);

            AuthWindow authWindow = new AuthWindow() { Owner = this };
            authWindow.ShowDialog();
            this.CurrentUser = authWindow.AuthUser;

            if (this.CurrentUser is null)
                this.Close();
        }

        private void SendMenuItem_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
