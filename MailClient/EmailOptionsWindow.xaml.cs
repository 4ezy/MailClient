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
    /// Логика взаимодействия для EmailOptions.xaml
    /// </summary>
    public partial class EmailOptionsWindow : Window
    {
        public EmailBox EmailBox { get; set; }

        public EmailOptionsWindow()
        {
            InitializeComponent();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            if (imapServerAddressTextBox.Text == String.Empty ||
                imapPortTextBox.Text == String.Empty ||
                smtpServerAddressTextBox.Text == String.Empty ||
                smtpPortTextBox.Text == String.Empty ||
                emailAddressTextBox.Text == String.Empty ||
                passwordPasswordBox.Password == String.Empty)
            {
                MessageBox.Show("Все поля обязательны для заполнения.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                EmailBox emailBox;
                try
                {
                    emailBox = new EmailBox(emailAddressTextBox.Text, passwordPasswordBox.Password,
                    imapServerAddressTextBox.Text, Convert.ToInt32(imapPortTextBox.Text),
                    smtpServerAddressTextBox.Text, Convert.ToInt32(smtpPortTextBox.Text));
                }
                catch (FormatException)
                {
                    MessageBox.Show("Номер порта должен быть числом.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                bool isConnected = emailBox.CheckConnection();

                if (isConnected)
                {
                    this.EmailBox = emailBox;
                    this.Close();
                }
                else
                    MessageBox.Show("При проверке данных была выявлена ошибка." +
                        " Проверьте правильность введённых данных.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (EmailBox != null)
            {
                imapServerAddressTextBox.Text = EmailBox.ImapServerAddress;
                imapPortTextBox.Text = Convert.ToString(EmailBox.ImapPort);
                smtpServerAddressTextBox.Text = EmailBox.SmtpServerAddress;
                smtpPortTextBox.Text = Convert.ToString(EmailBox.SmtpPort);
                emailAddressTextBox.Text = EmailBox.EmailAddress;
                passwordPasswordBox.Password = EmailBox.Password;
            }
        }
    }
}
