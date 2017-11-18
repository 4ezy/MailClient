using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace MailClient
{
    /// <summary>
    /// Логика взаимодействия для RegistrationWindow.xaml
    /// </summary>
    public partial class RegistrationWindow : Window
    {
        private User _registredUser;
        public User RegistredUser { get => _registredUser; }

        public RegistrationWindow()
        {
            InitializeComponent();
        }

        private void ApplyRegistrationButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.nameTextBox.Text == String.Empty ||
                this.loginTextBox.Text == String.Empty ||
                this.passwordTextBox.Password == String.Empty)
            {
                MessageBox.Show("Все поля обязательны для заполнения.", "Ошибка");
            }
            else
            {
                User user = new User
                {
                    Name = this.nameTextBox.Text,
                    Login = this.loginTextBox.Text,
                    Password = this.passwordTextBox.Password
                };

                bool userExists = false;

                for (int i = 0; i < ((AuthWindow)this.Owner).Users.Count && userExists == false; i++)
                {
                    if (((AuthWindow)this.Owner).Users[i].Login == user.Login)
                        userExists = true;
                }

                if (userExists)
                {
                    MessageBox.Show("Пользователь с таким логином уже существует.", "Ошибка");
                }
                else
                {
                    //((MainWindow)((AuthWindow)this.Owner).Owner).CurrentUser = registredUser;
                    //string path = @"C:\Users\" + Environment.UserName + @"\MailClient\userdata.mcd";
                    //BinarySerializer.Serialize(((MainWindow)Owner).CurrentUser, path);
                    this._registredUser = user;
                    this.Close();
                }
            }
        }
    }
}
