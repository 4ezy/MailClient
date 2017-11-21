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
        public List<User> Users { get; private set; }
        public static readonly string UsersDataPath = "usersdata.mcd";

        public AuthWindow()
        {
            this.InitializeComponent();

            if (File.Exists(AuthWindow.UsersDataPath))
            {
                byte[] data = Encrypter.AesDecryptFile(AuthWindow.UsersDataPath,
                    Encrypter.DefaultKey, Encrypter.DefaultIV);
                this.Users = BinarySerializer.Deserialize<List<User>>(data);
            }
            else
                this.Users = new List<User>();
        }

        private void RegistrationButton_Click(object sender, RoutedEventArgs e)
        {
            RegistrationWindow registrationWindow = new RegistrationWindow() { Owner = this };
            registrationWindow.ShowDialog();
            this.AuthUser = registrationWindow.RegistredUser;
            if (this.AuthUser != null)
            {
                this.Users.Add(this.AuthUser);
                byte[] serData = BinarySerializer.Serialize(this.Users);
                byte[] encSerData = Encrypter.AesEncrypt(serData,
                    Encrypter.DefaultKey, Encrypter.DefaultIV);
                File.WriteAllBytes(AuthWindow.UsersDataPath, encSerData);
                this.Close();
            }            
        }

        private void EnterButton_Click(object sender, RoutedEventArgs e)
        {
            bool userExists = false;

            for (int i = 0; i < this.Users.Count && userExists == false; i++)
            {
                if (this.Users[i].Login == loginTextBox.Text &&
                    this.Users[i].Password == passwordTextBox.Password)
                {
                    this.AuthUser = this.Users[i];
                    userExists = true;
                }
            }

            if (userExists)
            {
                if (remeberMeCheckBox.IsChecked == true)
                {
                    byte[] serData = BinarySerializer.Serialize(this.AuthUser);
                    byte[] encSerData = Encrypter.AesEncrypt(serData,
                        Encrypter.DefaultKey, Encrypter.DefaultIV);
                    File.WriteAllBytes(MainWindow.UserDataPath, encSerData);
                }
                    

                this.Close();
            }
            else
            {
                MessageBox.Show("Такого пользователя не существует!", "Ошибка");
            }
        }
    }
}
