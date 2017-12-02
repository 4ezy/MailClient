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
    /// Логика взаимодействия для AppOptionsWindow.xaml
    /// </summary>
    public partial class AppOptionsWindow : Window
    {
        public User User { get; set; }

        public AppOptionsWindow(User currentUser)
        {
            this.InitializeComponent();
            this.User = currentUser;
        }

        private void AddEmailButton_Click(object sender, RoutedEventArgs e)
        {
            EmailOptionsWindow emailOptionsWindow = new EmailOptionsWindow() { Owner = this };
            emailOptionsWindow.ShowDialog();

            if (emailOptionsWindow.EmailBox != null)
            {
                if (!this.User.EmailBoxes.Contains(emailOptionsWindow.EmailBox))
                {
                    this.User.EmailBoxes.Add(emailOptionsWindow.EmailBox);
                    this.User.SelectedEmailBoxIndex = this.User.EmailBoxes.Count > 0 ?
                        this.User.SelectedEmailBoxIndex : -1;
                    this.emailAccountsListBox.Items.Add(emailOptionsWindow.EmailBox.EmailAddress);
                }
                else
                {
                    MessageBox.Show("Почтовый ящик с таким адресом уже существует.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.nameTextBox.Text == String.Empty &&
                this.loginTextBox.Text == String.Empty &&
                this.passwordBox.Password == String.Empty)
            {
                MessageBox.Show("Имя, логин и пароль не могут быть пустыми.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                this.User.Name = nameTextBox.Text;
                this.User.Login = loginTextBox.Text;
                this.User.Password = passwordBox.Password;
                this.Close();
            }
        }

        private void DeleteEmailButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.emailAccountsListBox.SelectedIndex != -1)
            {
                if (MessageBox.Show("Вы уверены, что хотите удалить данные о почтовом ящике?", "Внимание",
                MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
                {
                    this.User.SelectedEmailBoxIndex = -1;
                    this.User.EmailBoxes.RemoveAt(this.emailAccountsListBox.SelectedIndex);
                    this.emailAccountsListBox.Items.RemoveAt(this.emailAccountsListBox.SelectedIndex);
                }
            }
            else
                MessageBox.Show("Для удаления требуется выбрать почтовый ящик.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ChangeEmailButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.emailAccountsListBox.SelectedIndex != -1)
            {
                EmailOptionsWindow emailOptionsWindow = new EmailOptionsWindow(
                    this.User.EmailBoxes[this.emailAccountsListBox.SelectedIndex])
                {
                    Owner = this
                };
                emailOptionsWindow.ShowDialog();
                this.User.EmailBoxes[this.emailAccountsListBox.SelectedIndex] = emailOptionsWindow.EmailBox;
                this.emailAccountsListBox.Items[this.emailAccountsListBox.SelectedIndex] =
                    emailOptionsWindow.EmailBox.EmailAddress;
                //((MainWindow)this.Owner).emailAccountsComboBox.Items[emailAccountsListBox.SelectedIndex] = 
                //    emailOptionsWindow.EmailBox.EmailAddress;
            }
            else
                MessageBox.Show("Для изменения требуется выбрать почтовый ящик.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.RefreshTextBoxesByUserData();
            this.RefreshListBoxByUserEmailBoxes();
        }

        private void RefreshTextBoxesByUserData()
        {
            this.nameTextBox.Clear();
            this.loginTextBox.Clear();
            this.passwordBox.Clear();

            this.nameTextBox.Text = this.User.Name;
            this.loginTextBox.Text = this.User.Login;
            this.passwordBox.Password = this.User.Password;
        }

        private void RefreshListBoxByUserEmailBoxes()
        {
            this.emailAccountsListBox.Items.Clear();

            foreach (EmailBox emailBox in this.User.EmailBoxes)
            {
                this.emailAccountsListBox.Items.Add(emailBox.EmailAddress);
            }
        }
    }
}
