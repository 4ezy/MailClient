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
        private User _authUser;
        public User AuthUser => _authUser;

        private List<User> _users;
        public List<User> Users => _users;

        private readonly string _usersDataPath = "usersdata.mcd";
        public string UsersDataPath => _usersDataPath;

        public AuthWindow()
        {
            this.InitializeComponent();

            if (File.Exists(_usersDataPath))
                _users = BinarySerializer.Deserialize<List<User>>(_usersDataPath);
            else
                _users = new List<User>();
        }

        private void RegistrationButton_Click(object sender, RoutedEventArgs e)
        {
            RegistrationWindow registrationWindow = new RegistrationWindow() { Owner = this };
            registrationWindow.ShowDialog();
            _authUser = registrationWindow.RegistredUser;
            if (_authUser != null)
            {
                _users.Add(_authUser);
                BinarySerializer.Serialize(_users, _usersDataPath);
                this.Close();
            }            
        }

        private void EnterButton_Click(object sender, RoutedEventArgs e)
        {
            bool userExists = false;

            for (int i = 0; i < _users.Count && userExists == false; i++)
            {
                if (_users[i].Login == loginTextBox.Text &&
                    _users[i].Password == passwordTextBox.Password)
                {
                    _authUser = _users[i];
                    userExists = true;
                }
            }

            if (userExists)
            {
                if (remeberMeCheckBox.IsChecked == true)
                    BinarySerializer.Serialize(_authUser,
                        ((MainWindow)this.Owner).UserDataPath);

                this.Close();
            }
            else
            {
                MessageBox.Show("Такого пользователя не существует!", "Ошибка");
            }
        }
    }
}
