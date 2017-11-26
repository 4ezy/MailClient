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

        public AppOptionsWindow()
        {
            InitializeComponent();
        }

        private void RefreshTextBoxesByUserData()
        {
            nameTextBox.Text = this.User.Name;
            loginTextBox.Text = this.User.Login;
            passwordBox.Password = this.User.Password;
        }

        private void RefreshListBoxByUserEmailBoxes()
        {
            if (this.User.EmailBoxes != null)
            {
                foreach (EmailBox emailBox in this.User.EmailBoxes)
                {
                    emailAccountsListBox.Items.Add(emailBox.EmailAddress);
                }
            }
        }

        private void AddEmailButton_Click(object sender, RoutedEventArgs e)
        {
            EmailOptionsWindow emailOptionsWindow = new EmailOptionsWindow() { Owner = this };
            emailOptionsWindow.ShowDialog();

            if (emailOptionsWindow.EmailBox != null)
            {
                if (this.User.EmailBoxes is null)
                    this.User.EmailBoxes = new List<EmailBox>();

                if (!this.User.EmailBoxes.Contains(emailOptionsWindow.EmailBox))
                {
                    this.User.EmailBoxes.Add(emailOptionsWindow.EmailBox);
                    emailAccountsListBox.Items.Add(
                        this.User.EmailBoxes[this.User.EmailBoxes.Count - 1].EmailAddress);
                }
                else
                {
                    MessageBox.Show("Почтовый ящик с таким адресом уже существует.", "Ошибка");
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            if (nameTextBox.Text == String.Empty &&
                loginTextBox.Text == String.Empty &&
                passwordBox.Password == String.Empty)
            {
                MessageBox.Show("Текстовые поля не могут быть пустыми.", "Ошибка");
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
            if (emailAccountsListBox.SelectedIndex != -1)
            {
                if (MessageBox.Show("Вы уверены, что хотите удалить данные о почтовом ящике?", "Внимание",
                MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
                {
                    this.User.EmailBoxes.RemoveAt(emailAccountsListBox.SelectedIndex);
                    emailAccountsListBox.Items.RemoveAt(emailAccountsListBox.SelectedIndex);
                }
            }
            else
                MessageBox.Show("Для изменения требуется выбрать почтовый ящик.", "Ошибка");
        }

        private void ChangeEmailButton_Click(object sender, RoutedEventArgs e)
        {
            if (emailAccountsListBox.SelectedIndex != -1)
            {
                EmailOptionsWindow emailOptionsWindow = new EmailOptionsWindow()
                {
                    Owner = this,
                    EmailBox = this.User.EmailBoxes[emailAccountsListBox.SelectedIndex]
                };
                emailOptionsWindow.ShowDialog();
            }
            else
                MessageBox.Show("Для изменения требуется выбрать почтовый ящик.", "Ошибка");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshTextBoxesByUserData();
            RefreshListBoxByUserEmailBoxes();
        }
    }
}
