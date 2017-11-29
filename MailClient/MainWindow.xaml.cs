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
        private static readonly string userDirectoryPath = @"C:\Users\" + Environment.UserName + @"\MailClient\";
        private static readonly string rememberMeDataPath = MainWindow.UserDirectoryPath + "remdata.mcd";
        public static string UserDirectoryPath => userDirectoryPath;
        public static string RememberMeDataPath => rememberMeDataPath;

        public User CurrentUser { get; set; }

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
                byte[] encryptedRememberMeData = File.ReadAllBytes(MainWindow.RememberMeDataPath);
                byte[] rememberMeData = Encrypter.DecryptWithAesAndRsa(encryptedRememberMeData, Encrypter.DefaultKeyContainerName);
                string rememberMeLogin = BinarySerializer.Deserialize<string>(rememberMeData);
                byte[] encryptedUserData = File.ReadAllBytes(MainWindow.UserDirectoryPath + rememberMeLogin + ".mcd");
                byte[] userData = Encrypter.DecryptWithAesAndRsa(encryptedUserData, Encrypter.DefaultKeyContainerName);
                this.CurrentUser = BinarySerializer.Deserialize<User>(userData);
            }
            else
            {
                AuthWindow authWindow = new AuthWindow() { Owner = this };
                authWindow.ShowDialog();
                this.CurrentUser = authWindow.AuthUser;
            }

            if (this.CurrentUser != null)
            {
                if (this.CurrentUser.EmailBoxes != null)
                {
                    emailAccountsComboBox.Items.Clear();

                    foreach (EmailBox emailBox in this.CurrentUser.EmailBoxes)
                    {
                        this.emailAccountsComboBox.Items.Add(emailBox.EmailAddress);
                        this.emailAccountsComboBox.SelectedIndex = emailAccountsComboBox.Items.Count != 0
                            ? this.CurrentUser.SelectedEmailBoxIndex : -1;
                    }
                }
            }
            else
                this.Close();
        }

        private void OptionsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AppOptionsWindow optionsWindow = new AppOptionsWindow()
            {
                Owner = this,
                User = (User)this.CurrentUser.Clone()
            };
            optionsWindow.ShowDialog();
            
            if (!this.CurrentUser.Equals(optionsWindow.User))
            {
                File.Delete(MainWindow.UserDirectoryPath + this.CurrentUser.Login + ".mcd");
                this.CurrentUser = optionsWindow.User;
                byte[] serData = BinarySerializer.Serialize(this.CurrentUser);
                byte[] encSerData = Encrypter.EncryptWithAesAndRsa(serData, Encrypter.DefaultKeyContainerName);
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

        private void EmailAccountsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.CurrentUser.SelectedEmailBoxIndex = this.emailAccountsComboBox.SelectedIndex;
        }
    }
}
