﻿using Limilabs.Client;
using Limilabs.Client.IMAP;
using Limilabs.Mail;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
        private readonly int maxMessages = 20;
        private Thread inboxThread;
        private MessagesType messagesType = MessagesType.Inbox;
        private int lastSelectedTabItemIdex = -1;
        private List<long> uids;
        public MainWindow()
        {
            this.InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.MailClientInitializationData();
        }

        private void OptionsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (inboxThread != null && inboxThread.IsAlive)
            {
                inboxThread.Abort();
                inboxThread.Join();
            }

            AppOptionsWindow optionsWindow = new AppOptionsWindow(
                (User)this.CurrentUser.Clone())
            {
                Owner = this
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
            if (inboxThread != null && inboxThread.IsAlive)
            {
                inboxThread.Abort();
                inboxThread.Join();
            }

            this.ClearUIData();
            this.CurrentUser = null;

            if (File.Exists(MainWindow.RememberMeDataPath))
                File.Delete(MainWindow.RememberMeDataPath);

            this.MailClientInitializationData();

            if (this.CurrentUser.SelectedEmailBoxIndex != -1)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                //this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex].ConnectImap();
                Mouse.OverrideCursor = null;
                this.messagesOffset = 0;
                //this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex].ChangeFolder(messagesType);
                //this.DownloadMessagesToClient(messagesType);
            }
        }

        private void EmailAccountsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (inboxThread != null && inboxThread.IsAlive)
            {
                inboxThread.Abort();
                inboxThread.Join();
            }

            this.CurrentUser.SelectedEmailBoxIndex = this.emailAccountsComboBox.SelectedIndex;

            if (this.CurrentUser.SelectedEmailBoxIndex != -1)
            {
                this.inboxListBox.Items.Clear();
                this.sentListBox.Items.Clear();
                this.draftsListBox.Items.Clear();
                this.basketListBox.Items.Clear();

                this.messagesOffset = 0;
                Task.Factory.StartNew(() =>
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        Mouse.OverrideCursor = Cursors.Wait;
                    });
                    this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex].ConnectImap();
                    this.Dispatcher.Invoke(() =>
                    {
                        Mouse.OverrideCursor = null;
                    });
                    this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex].ChangeFolder(messagesType);
                    this.DownloadMessagesToClient(messagesType);
                });
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
                byte[] rememberMeData = Encrypter.DecryptWithAesAndRsa(encryptedRememberMeData, Encrypter.DefaultKeyContainerName, false);
                string rememberMeLogin = BinarySerializer.Deserialize<string>(rememberMeData);

                if (File.Exists(MainWindow.UserDirectoryPath + rememberMeLogin + ".mcd"))
                {
                    byte[] encryptedUserData = File.ReadAllBytes(MainWindow.UserDirectoryPath + rememberMeLogin + ".mcd");
                    byte[] userData = Encrypter.DecryptWithAesAndRsa(encryptedUserData, Encrypter.DefaultKeyContainerName, false);
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
            byte[] encSerData = Encrypter.EncryptWithAesAndRsa(serData, Encrypter.DefaultKeyContainerName, false);
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

        private void DownloadMessagesToClient(MessagesType messagesType)
        {
            if (inboxThread != null && inboxThread.IsAlive)
            {
                inboxThread.Abort();
                inboxThread.Join();
            }

            if (this.CurrentUser.EmailBoxes.Count != 0)
            {
                inboxThread = new Thread(() =>
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        this.uids = new List<long>();
                        this.Cursor = Cursors.AppStarting;
                        if (messagesType == MessagesType.Inbox)
                            this.inboxListBox.Items.Clear();
                        else if (messagesType == MessagesType.Sent)
                            this.sentListBox.Items.Clear();
                        else if (messagesType == MessagesType.Drafts)
                            this.draftsListBox.Items.Clear();
                        else if (messagesType == MessagesType.Basket)
                            this.basketListBox.Items.Clear();
                    });

                    try
                    {
                        this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex].ChangeFolder(messagesType);
                        this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex].DownloadEnvelopes(
                            this.messagesOffset, this.maxMessages, MessagesBeginningFrom.New,
                            ((string subject, long uid) =>
                            {
                                this.Dispatcher.Invoke(() =>
                                {
                                    if (messagesType == MessagesType.Inbox)
                                        this.inboxListBox.Items.Add(subject);
                                    else if (messagesType == MessagesType.Sent)
                                        this.sentListBox.Items.Add(subject);
                                    else if (messagesType == MessagesType.Drafts)
                                        this.draftsListBox.Items.Add(subject);
                                    else if (messagesType == MessagesType.Basket)
                                        this.basketListBox.Items.Add(subject);
                                });

                                this.uids.Add(uid);
                            }));
                    }
                    catch (ServerException)
                    {
                        this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex].ConnectImap();
                        this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex].ChangeFolder(messagesType);
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
            if (this.CurrentUser.EmailBoxes[
                this.CurrentUser.SelectedEmailBoxIndex].Imap.CurrentFolder != null)
            {
                if (inboxThread != null && inboxThread.IsAlive)
                {
                    inboxThread.Abort();
                    inboxThread.Join();
                }

                this.messagesOffset = 0;

                this.toStartButton.IsEnabled = false;
                this.backButton.IsEnabled = false;
                this.nextButton.IsEnabled = true;
                this.toEndButton.IsEnabled = true;

                this.DownloadMessagesToClient(messagesType);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex].Imap.CurrentFolder != null)
            {
                if (inboxThread != null && inboxThread.IsAlive)
                {
                    inboxThread.Abort();
                    inboxThread.Join();
                }

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
                this.DownloadMessagesToClient(messagesType);
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex].Imap.CurrentFolder != null)
            {
                if (inboxThread != null && inboxThread.IsAlive)
                {
                    inboxThread.Abort();
                    inboxThread.Join();
                }

                int messageCount = this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex].Imap.Search(Flag.All).Count;
                int messageBorder = messageCount / this.maxMessages * this.maxMessages;
                this.messagesOffset += this.maxMessages;

                if (this.messagesOffset < messageBorder)
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

                this.DownloadMessagesToClient(messagesType);
            }
        }

        private void ToEndButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex].Imap.CurrentFolder != null)
            {
                if (inboxThread != null && inboxThread.IsAlive)
                {
                    inboxThread.Abort();
                    inboxThread.Join();
                }

                int messageCount = this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex].Imap.Search(Flag.All).Count;

                this.messagesOffset = messageCount / this.maxMessages * this.maxMessages;

                this.toStartButton.IsEnabled = true;
                this.backButton.IsEnabled = true;
                this.nextButton.IsEnabled = false;
                this.toEndButton.IsEnabled = false;

                DownloadMessagesToClient(messagesType);
            } 
        }

        private void Changed_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (changed.SelectedIndex == this.lastSelectedTabItemIdex)
                return;
            else
                this.lastSelectedTabItemIdex = changed.SelectedIndex;

            if (inboxThread != null && inboxThread.IsAlive)
            {
                inboxThread.Abort();
                inboxThread.Join();
            }

            if (this.CurrentUser != null)
            {
                switch (changed.SelectedIndex)
                {
                    case 0:
                        messagesType = MessagesType.Inbox;
                        break;
                    case 1:
                        messagesType = MessagesType.Sent;
                        break;
                    case 2:
                        messagesType = MessagesType.Drafts;
                        break;
                    case 3:
                        messagesType = MessagesType.Basket;
                        break;
                    default:
                        break;
                }

                if (this.CurrentUser.SelectedEmailBoxIndex != -1)
                {
                    this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex].ChangeFolder(messagesType);
                    int messageCount = this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex].Imap.Search(Flag.All).Count;

                    if (messageCount < this.maxMessages)
                    {
                        this.toStartButton.IsEnabled = false;
                        this.backButton.IsEnabled = false;
                        this.nextButton.IsEnabled = false;
                        this.toEndButton.IsEnabled = false;
                    }
                    else
                    {
                        this.toStartButton.IsEnabled = false;
                        this.backButton.IsEnabled = false;
                        this.nextButton.IsEnabled = true;
                        this.toEndButton.IsEnabled = true;
                    }

                    this.messagesOffset = 0;
                    this.DownloadMessagesToClient(messagesType);
                }
            }
        }

        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (inboxThread != null && inboxThread.IsAlive)
            {
                inboxThread.Abort();
                inboxThread.Join();
            }

            long messageUid = -1;
            switch (messagesType)
            {
                case MessagesType.Inbox:
                    messageUid = this.uids[this.inboxListBox.SelectedIndex];
                    break;
                case MessagesType.Sent:
                    messageUid = this.uids[this.sentListBox.SelectedIndex];
                    break;
                case MessagesType.Drafts:
                    messageUid = this.uids[this.draftsListBox.SelectedIndex];
                    break;
                case MessagesType.Basket:
                    messageUid = this.uids[this.basketListBox.SelectedIndex];
                    break;
                default:
                    break;
            }

            if (messageUid != -1)
            {
                switch (messagesType)
                {
                    case MessagesType.Inbox:
                        IMail inboxMail = this.CurrentUser.EmailBoxes[
                            this.CurrentUser.SelectedEmailBoxIndex].DownloadMessage(messageUid);
                        InboxMailReadingWindow inboxMailReadingWindow = new InboxMailReadingWindow(inboxMail,
                            this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex])
                        {
                            Owner = this
                        };
                        inboxMailReadingWindow.Show();
                        inboxMailReadingWindow.Closed += ((object sndr, EventArgs evArg) =>
                        {
                            this.DownloadMessagesToClient(this.messagesType);
                        });
                        break;
                    case MessagesType.Sent:
                        IMail sentMail = this.CurrentUser.EmailBoxes[
                            this.CurrentUser.SelectedEmailBoxIndex].DownloadMessage(messageUid);
                        SentMailReadingWindow sentMailReadingWindow = new SentMailReadingWindow(
                            sentMail, this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex])
                        {
                            Owner = this
                        };
                        sentMailReadingWindow.Show();
                        sentMailReadingWindow.Closed += ((object sndr, EventArgs evArg) =>
                        {
                            this.DownloadMessagesToClient(this.messagesType);
                        });
                        break;
                    case MessagesType.Drafts:
                        if (this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex].Smtp == null ||
                            !this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex].Smtp.Connected)
                        {
                            this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex].ConnectSmtp();
                        }

                        IMail draftMail = this.CurrentUser.EmailBoxes[
                            this.CurrentUser.SelectedEmailBoxIndex].DownloadMessage(messageUid);
                        DraftsMailWindow sendMailWindow = new DraftsMailWindow(
                            this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex],
                            draftMail, messageUid)
                        {
                            Owner = this
                        };
                        sendMailWindow.ShowDialog();
                        this.DownloadMessagesToClient(this.messagesType);
                        break;
                    case MessagesType.Basket:
                        IMail basketMail = this.CurrentUser.EmailBoxes[
                            this.CurrentUser.SelectedEmailBoxIndex].DownloadMessage(messageUid);
                        BasketMailReadingWindow basketMailReadingWindow = new BasketMailReadingWindow(
                            basketMail, this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex])
                        {
                            Owner = this
                        };
                        basketMailReadingWindow.Show();
                        basketMailReadingWindow.Closed += ((object sndr, EventArgs evArg) =>
                        {
                            this.DownloadMessagesToClient(this.messagesType);
                        });
                        break;
                    default:
                        break;
                }
            }
        }

        private void WriteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (inboxThread != null && inboxThread.IsAlive)
            {
                inboxThread.Abort();
                inboxThread.Join();
            }

            if (this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex].Smtp == null ||
                !this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex].Smtp.Connected)
            {
                this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex].ConnectSmtp();
            }

            SendMailWindow sendMailWindow = new SendMailWindow(
                this.CurrentUser.EmailBoxes[this.CurrentUser.SelectedEmailBoxIndex])
            {
                Owner = this
            };
            sendMailWindow.ShowDialog();
            this.DownloadMessagesToClient(this.messagesType);
        }

        private void ImportPublicCipherKeyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Title = "Импорт открытого ключа шифрования",
                Filter = "Открытый ключ шифрования (*.pubkc)|*.pubkc",
            };
            if (ofd.ShowDialog() == true)
            {
                string publicKey = File.ReadAllText(ofd.FileName);
                this.CurrentUser.EmailBoxes[
                            this.CurrentUser.SelectedEmailBoxIndex].XmlStringChipherKeyContainerName = publicKey;
            }
        }

        private void ImportPublicSignKeyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Title = "Импорт открытого ключа подписи",
                Filter = "Открытый ключ подписи (*.pubks)|*.pubks",
            };
            if (ofd.ShowDialog() == true)
            {
                string publicKey = File.ReadAllText(ofd.FileName);
                this.CurrentUser.EmailBoxes[
                            this.CurrentUser.SelectedEmailBoxIndex].XmlStringSignKeyContainerName = publicKey;
            }
        }

        private void ExportPublicCipherKeyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog()
            {
                Title = "Экспорт открытого ключа шифрования",
                Filter = "Открытый ключ шифрования (*.pubkc)|*.pubkc",
                FileName = this.CurrentUser.EmailBoxes[
                    this.CurrentUser.SelectedEmailBoxIndex].EmailAddress + ".pubkc",
            };
            if (sfd.ShowDialog() == true)
            {
                CspParameters cspp = new CspParameters
                {
                    KeyContainerName = this.CurrentUser.EmailBoxes[
                        this.CurrentUser.SelectedEmailBoxIndex].UserKeyContainerName
                };
                RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(cspp);
                string publicKey = rsa.ToXmlString(false);
                File.WriteAllText(sfd.FileName, publicKey);
            }
        }

        private void ExportPublicSignKeyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog()
            {
                Title = "Экспорт открытого ключа подписи",
                Filter = "Открытый ключ подписи (*.pubks)|*.pubks",
                FileName = this.CurrentUser.EmailBoxes[
                    this.CurrentUser.SelectedEmailBoxIndex].EmailAddress + ".pubks",
            };
            if (sfd.ShowDialog() == true)
            {
                DSACryptoServiceProvider dsa = new DSACryptoServiceProvider();

                if (!File.Exists(MainWindow.UserDirectoryPath + this.CurrentUser.EmailBoxes[
                    this.CurrentUser.SelectedEmailBoxIndex].UserKeyContainerName + ".akey"))
                {
                    File.WriteAllText(MainWindow.UserDirectoryPath + this.CurrentUser.EmailBoxes[
                        this.CurrentUser.SelectedEmailBoxIndex].UserKeyContainerName + ".akey",
                        dsa.ToXmlString(true));
                }
                else
                {
                    dsa.FromXmlString(File.ReadAllText(
                    MainWindow.UserDirectoryPath + this.CurrentUser.EmailBoxes[
                        this.CurrentUser.SelectedEmailBoxIndex].UserKeyContainerName + ".akey"));
                }

                File.WriteAllText(sfd.FileName, dsa.ToXmlString(false));
            }
        }
    }
}
