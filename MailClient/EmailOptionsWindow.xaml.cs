using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        public EmailBox EmailBox { get; private set; }
        Thread thread;

        public EmailOptionsWindow()
        {
            this.InitializeComponent();
        }

        public EmailOptionsWindow(EmailBox emailBox) : this()
        {
            this.EmailBox = emailBox;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.imapServerAddressTextBox.Text == String.Empty ||
                this.imapPortTextBox.Text == String.Empty ||
                this.smtpServerAddressTextBox.Text == String.Empty ||
                this.smtpPortTextBox.Text == String.Empty ||
                this.emailAddressTextBox.Text == String.Empty ||
                this.passwordPasswordBox.Password == String.Empty)
            {
                MessageBox.Show("Все поля обязательны для заполнения.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                EmailBox emailBox;
                try
                {
                    emailBox = new EmailBox(this.emailAddressTextBox.Text, this.passwordPasswordBox.Password,
                    this.imapServerAddressTextBox.Text, Convert.ToInt32(this.imapPortTextBox.Text),
                    this.smtpServerAddressTextBox.Text, Convert.ToInt32(this.smtpPortTextBox.Text));
                }
                catch (FormatException)
                {
                    MessageBox.Show("Номер порта должен быть целым числом.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                thread = new Thread(() =>
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        this.connectionInfoLabel.Content = "Попытка подключения к серверу...";
                        this.Cursor = Cursors.Wait;
                        this.cancelButton.Cursor = Cursors.Arrow;
                        this.acceptButton.IsEnabled = false;
                    });

                    bool isConnected = emailBox.Connect();

                    if (isConnected)
                    {
                        this.EmailBox = emailBox;
                        this.Dispatcher.Invoke(() =>
                        {
                            this.Close();
                        });
                    }
                    else
                    {
                        MessageBox.Show("Ошибка соединения с сервером." +
                            " Проверьте правильность введённых данных или интернет соеденение.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    this.Dispatcher.Invoke(() =>
                    {
                        this.connectionInfoLabel.Content = String.Empty;
                        this.Cursor = null;
                        this.cancelButton.Cursor = null;
                        this.acceptButton.IsEnabled = true;
                        
                    });
                }) { IsBackground = true, Name = "ConnectThread" };
                thread.Start();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.EmailBox != null)
            {
                imapServerAddressTextBox.Text = this.EmailBox.ImapServerAddress;
                imapPortTextBox.Text = Convert.ToString(this.EmailBox.ImapPort);
                smtpServerAddressTextBox.Text = this.EmailBox.SmtpServerAddress;
                smtpPortTextBox.Text = Convert.ToString(this.EmailBox.SmtpPort);
                emailAddressTextBox.Text = this.EmailBox.EmailAddress;
                passwordPasswordBox.Password = this.EmailBox.Password;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (thread != null && thread.IsAlive)
                thread.Abort();
        }
    }
}
