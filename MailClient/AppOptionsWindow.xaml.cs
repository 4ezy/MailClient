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
    /// Логика взаимодействия для OptionsWindow.xaml
    /// </summary>
    public partial class AppOptionsWindow : Window
    {
        public List<EmailBox> EmailBoxes { get; set; }

        public AppOptionsWindow()
        {
            InitializeComponent();
            UpdateComponents();
            EmailBoxes = ((MainWindow)this.Owner).CurrentUser.EmailBoxes;
            UpdateListBox();
        }

        private void AddEmailButton_Click(object sender, RoutedEventArgs e)
        {
            EmailOptionsWindow emailOptionsWindow = new EmailOptionsWindow() { Owner = this };
            emailOptionsWindow.ShowDialog();
            if (emailOptionsWindow.EmailBox != null)
            {
                if (!EmailBoxes.Contains(emailOptionsWindow.EmailBox))
                {
                    EmailBoxes.Add(emailOptionsWindow.EmailBox);
                    emailAccountsListBox.Items.Add(EmailBoxes[EmailBoxes.Count - 1].EmailAddress);
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
            if (nameTextBox.Text == String.Empty ||
                loginTextBox.Text == String.Empty ||
                passwordBox.Password == String.Empty)
            {
                MessageBox.Show("Текстовые поля не могут быть пустыми.", "Ошибка");
            }
            else
            {
                ((MainWindow)this.Owner).CurrentUser.Name = nameTextBox.Text;
                ((MainWindow)this.Owner).CurrentUser.Login = loginTextBox.Text;
                ((MainWindow)this.Owner).CurrentUser.Password = passwordBox.Password;
                ((MainWindow)this.Owner).CurrentUser.EmailBoxes = EmailBoxes;
            }
        }

        private void UpdateComponents()
        {
            nameTextBox.Text = ((MainWindow)this.Owner).CurrentUser.Name;
            loginTextBox.Text = ((MainWindow)this.Owner).CurrentUser.Login;
            passwordBox.Password = ((MainWindow)this.Owner).CurrentUser.Password;
        }

        private void UpdateListBox()
        {
            foreach (EmailBox emailBox in EmailBoxes)
            {
                emailAccountsListBox.Items.Add(emailBox.EmailAddress);
            }
        }

        private void DeleteEmailButton_Click(object sender, RoutedEventArgs e)
        {
            if (emailAccountsListBox.SelectedIndex != -1)
            {
                if (MessageBox.Show("Вы уверены, что хотите удалить данные о почтовом ящике?", "Внимание",
                MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
                {
                    EmailBoxes.RemoveAt(emailAccountsListBox.SelectedIndex);
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
                    EmailBox = this.EmailBoxes[emailAccountsListBox.SelectedIndex]
                };
                emailOptionsWindow.ShowDialog();
            }
            else
                MessageBox.Show("Для изменения требуется выбрать почтовый ящик.", "Ошибка");
        }
    }
}
