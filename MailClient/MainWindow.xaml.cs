using Limilabs.Client;
using Limilabs.Client.IMAP;
using Limilabs.Mail;
using System;
using System.Collections.Generic;
using System.IO;
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

        public User CurrentUser { get; private set; }
        private int messagesOffset = 0;
        private int maxMessages = 22;
        private Thread inboxThread;

        public MainWindow()
        {
            this.InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.MailClientInitializationData();
            Mouse.OverrideCursor = Cursors.Wait;
            this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex].Connect();
            Mouse.OverrideCursor = null;
            if (this.CurrentUser.SelectedEmailBoxIndex != -1)
            {
                this.messagesOffset = 0;
                this.DownloadMessagesToClient();
            }
        }

        private void OptionsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AppOptionsWindow optionsWindow = new AppOptionsWindow(this.CurrentUser)
            {
                Owner = this,
                User = (User)this.CurrentUser.Clone()
            };
            optionsWindow.ShowDialog();
            
            if (!this.CurrentUser.Equals(optionsWindow.User))
            {
                File.Delete(MainWindow.UserDirectoryPath + this.CurrentUser.Login + ".mcd");
                this.CurrentUser = optionsWindow.User;
                this.EmailComboBoxDataRefresh();
                this.EncryptAndSerializeCurrentUser();
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (this.inboxThread != null && this.inboxThread.IsAlive)
                inboxThread.Abort();

            this.ClearUIData();
            this.CurrentUser = null;

            if (File.Exists(MainWindow.RememberMeDataPath))
                File.Delete(MainWindow.RememberMeDataPath);

            this.MailClientInitializationData();
            Mouse.OverrideCursor = Cursors.Wait;
            this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex].Connect();
            Mouse.OverrideCursor = null;
        }

        private void EmailAccountsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.CurrentUser.SelectedEmailBoxIndex = this.emailAccountsComboBox.SelectedIndex;
            if (this.CurrentUser.SelectedEmailBoxIndex != -1)
            {
                this.messagesOffset = 0;
                this.DownloadMessagesToClient();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.CurrentUser != null)
            {
                this.EncryptAndSerializeCurrentUser();
            }
        }

        private void EmailComboBoxDataRefresh()
        {
            int selIndexBuf = this.CurrentUser.SelectedEmailBoxIndex;
            this.emailAccountsComboBox.Items.Clear();
            this.CurrentUser.SelectedEmailBoxIndex = selIndexBuf;

            foreach (EmailBox emailBox in this.CurrentUser.EmailBoxes)
            {
                this.emailAccountsComboBox.Items.Add(emailBox.EmailAddress);
            }
            
            this.emailAccountsComboBox.SelectedIndex = this.CurrentUser.SelectedEmailBoxIndex;
        }

        private void UserDataReading()
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

                if (File.Exists(MainWindow.UserDirectoryPath + rememberMeLogin + ".mcd"))
                {
                    byte[] encryptedUserData = File.ReadAllBytes(MainWindow.UserDirectoryPath + rememberMeLogin + ".mcd");
                    byte[] userData = Encrypter.DecryptWithAesAndRsa(encryptedUserData, Encrypter.DefaultKeyContainerName);
                    this.CurrentUser = BinarySerializer.Deserialize<User>(userData);
                }
                else
                    File.Delete(MainWindow.RememberMeDataPath);
            }
            else
            {
                AuthWindow authWindow = new AuthWindow() { Owner = this };
                authWindow.ShowDialog();
                this.CurrentUser = authWindow.AuthUser;
            }
        }

        private void EncryptAndSerializeCurrentUser()
        {
            byte[] serData = BinarySerializer.Serialize(this.CurrentUser);
            byte[] encSerData = Encrypter.EncryptWithAesAndRsa(serData, Encrypter.DefaultKeyContainerName);
            File.WriteAllBytes(MainWindow.UserDirectoryPath + this.CurrentUser.Login + ".mcd", encSerData);
        }

        private void MailClientInitializationData()
        {
            this.UserDataReading();

            if (this.CurrentUser != null)
            {
                this.EmailComboBoxDataRefresh();
            }
            else
                this.Close();
        }

        private void DownloadMessagesToClient()
        {
            if (inboxThread != null && inboxThread.IsAlive)
                inboxThread.Abort();

            if (this.CurrentUser.EmailBoxes.Count != 0)
            {
                inboxThread = new Thread(() =>
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        this.Cursor = Cursors.AppStarting;
                        this.inboxListBox.Items.Clear();
                    });

                    try
                    {
                        this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex].DownloadInboxMessages(
                            this.messagesOffset, this.maxMessages, MessagesType.Inbox, MessagesBeginningFrom.New,
                            ((string subject) =>
                            {
                                this.Dispatcher.Invoke(() =>
                                {
                                    this.inboxListBox.Items.Add(subject);
                                });
                            }));
                    }
                    catch (ServerException)
                    {
                        MessageBox.Show("Ошибка скачивания сообщений. Пожалуйста, попробуйте позже.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    this.Dispatcher.Invoke(() =>
                    {
                        this.Cursor = null;
                    });
                }) { IsBackground = true, Name = "EmailDownloadThread" };

                inboxThread.Start();
            }
        }

        private void ClearUIData()
        {
            this.inboxListBox.Items.Clear();
            this.emailAccountsComboBox.Items.Clear();
        }

        private void ToStartButton_Click(object sender, RoutedEventArgs e)
        {
            this.messagesOffset = 0;

            this.toStartButton.IsEnabled = false;
            this.backButton.IsEnabled = false;
            this.nextButton.IsEnabled = true;
            this.toEndButton.IsEnabled = true;

            this.DownloadMessagesToClient();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.messagesOffset > this.maxMessages)
            {
                this.toStartButton.IsEnabled = true;
                this.backButton.IsEnabled = true;
            }
            else
            {
                this.toStartButton.IsEnabled = false;
                this.backButton.IsEnabled = false;
            }

            this.nextButton.IsEnabled = true;
            this.toEndButton.IsEnabled = true;
            this.messagesOffset -= this.maxMessages;
            this.DownloadMessagesToClient();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            int messageCount = this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex].Imap.Search(Flag.All).Count;

            if (this.messagesOffset < messageCount)
            {
                this.nextButton.IsEnabled = true;
                this.toEndButton.IsEnabled = true;
            }
            else
            {
                this.nextButton.IsEnabled = false;
                this.toEndButton.IsEnabled = false;
            }

            this.toStartButton.IsEnabled = true;
            this.backButton.IsEnabled = true;
            this.messagesOffset += this.maxMessages;
            this.DownloadMessagesToClient();
        }

        private void ToEndButton_Click(object sender, RoutedEventArgs e)
        {
            int messageCount = this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex].Imap.Search(Flag.All).Count;

            this.messagesOffset = messageCount / this.maxMessages * this.maxMessages;

            this.toStartButton.IsEnabled = true;
            this.backButton.IsEnabled = true;
            this.nextButton.IsEnabled = false;
            this.toEndButton.IsEnabled = false;

            DownloadMessagesToClient();
        }
    }
}
