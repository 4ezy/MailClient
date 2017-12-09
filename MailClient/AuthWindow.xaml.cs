using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MailClient
{
    /// <summary>
    /// Логика взаимодействия для AuthWindow.xaml
    /// </summary>
    public partial class AuthWindow : Window
    {
        public User AuthUser { get; private set; }

        public AuthWindow()
        {
            this.InitializeComponent();
        }

        private void RegistrationButton_Click(object sender, RoutedEventArgs e)
        {
            RegistrationWindow registrationWindow = new RegistrationWindow() { Owner = this };
            registrationWindow.ShowDialog();
            this.AuthUser = registrationWindow.RegistredUser;
            if (this.AuthUser != null)
            {
                byte[] serData = BinarySerializer.Serialize(this.AuthUser);
                byte[] encSerData = Encrypter.EncryptWithAesAndRsa(serData, Encrypter.DefaultKeyContainerName, false);
                File.WriteAllBytes(MainWindow.UserDirectoryPath + this.AuthUser.Login + ".mcd", encSerData);
                this.Close();
            }            
        }

        private void EnterButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(MainWindow.UserDirectoryPath + this.loginTextBox.Text + ".mcd"))
            {
                byte[] userEncryptedData = File.ReadAllBytes(MainWindow.UserDirectoryPath +
                    this.loginTextBox.Text + ".mcd");
                byte[] userData = Encrypter.DecryptWithAesAndRsa(userEncryptedData, Encrypter.DefaultKeyContainerName, false);
                User user = BinarySerializer.Deserialize<User>(userData);

                if (user.Password == this.passwordTextBox.Password)
                {
                    this.AuthUser = user;

                    if (this.remeberMeCheckBox.IsChecked == true)
                    {
                        byte[] serData = BinarySerializer.Serialize(user.Login);
                        byte[] encSerData = Encrypter.EncryptWithAesAndRsa(serData, Encrypter.DefaultKeyContainerName, false);
                        File.WriteAllBytes(MainWindow.RememberMeDataPath, encSerData);
                    }

                    this.Close();
                }
                else
                    MessageBox.Show("Неправильный пароль!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                MessageBox.Show("Такого пользователя не существует!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
