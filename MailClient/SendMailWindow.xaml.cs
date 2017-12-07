using Limilabs.Client.IMAP;
using Limilabs.Client.SMTP;
using Limilabs.Mail;
using Limilabs.Mail.Headers;
using Limilabs.Mail.MIME;
using Microsoft.Win32;
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
using System.Windows.Shapes;

namespace MailClient
{
    /// <summary>
    /// Логика взаимодействия для SendMailWindow.xaml
    /// </summary>
    public partial class SendMailWindow : Window
    {
        private EmailBox EmailBox { get; set; }
        private List<byte[]> Attachments { get; set; }
        Thread sendThread;

        public SendMailWindow()
        {
            this.InitializeComponent();
        }

        public SendMailWindow(EmailBox emailBox)
        {
            this.InitializeComponent();
            this.EmailBox = emailBox;
            this.Attachments = new List<byte[]>();
        }

        private void LoadAttachmentsButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Multiselect = true,
                Filter = "Все файлы (*.*)|*.*",
                Title = "Выберите файлы"
            };
            if (ofd.ShowDialog() == true)
            {
                for (int i = 0; i < ofd.FileNames.Length; i++)
                {
                    byte[] fileData = File.ReadAllBytes(ofd.FileNames[i]);

                    if (fileData.Length < 10000000)
                    {
                        this.Attachments.Add(fileData);
                        this.attachmentsListBox.Items.Add(ofd.SafeFileNames[i]);
                    }
                    else
                        MessageBox.Show(String.Format("Файл {0} слишком большой. Можно передавать файлы размером до 10 МБ.", ofd.SafeFileName[i]),
                            "Ошибка",MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteAttachmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.attachmentsListBox.SelectedIndex != -1)
            {
                this.Attachments.RemoveAt(this.attachmentsListBox.SelectedIndex);
                this.attachmentsListBox.Items.RemoveAt(this.attachmentsListBox.SelectedIndex);
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.toTextBox.Text is null)
            {
                MessageBox.Show("Требуется ввести имя получателя", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MailBuilder mailBuilder = new MailBuilder();
            mailBuilder.From.Add(new MailBox(this.EmailBox.EmailAddress));
            mailBuilder.To.Add(new MailBox(this.toTextBox.Text.Trim(' ')));

            if (this.encryptMessage.IsChecked == false)
            {
                mailBuilder.Subject = this.subjectTextBox.Text;
            }
            else
            {
                string data = this.subjectTextBox.Text;
                Encoding encoding = Encoding.GetEncoding(0);
                byte[] encrData = Encrypter.EncryptWithAesAndRsa(encoding.GetBytes(data),
                    this.EmailBox.UserKeyContainerName);
                byte[] signedData = Encrypter.SignData(encrData, 
                    this.EmailBox.UserKeyContainerName);
                mailBuilder.Subject = BitConverter.ToString(signedData);
            }

            if (this.encryptMessage.IsChecked == false)
            {
                mailBuilder.Rtf = new TextRange(this.textRichTextBox.Document.ContentStart,
                   this.textRichTextBox.Document.ContentEnd).Text;
            }
            else
            {
                string data = new TextRange(this.textRichTextBox.Document.ContentStart,
                   this.textRichTextBox.Document.ContentEnd).Text;
                Encoding encoding = Encoding.GetEncoding(0);
                byte[] encrData = Encrypter.EncryptWithAesAndRsa(encoding.GetBytes(data),
                    this.EmailBox.UserKeyContainerName);
                byte[] signedData = Encrypter.SignData(encrData,
                    this.EmailBox.UserKeyContainerName);
                mailBuilder.Rtf = BitConverter.ToString(signedData);
            }

            for (int i = 0; i < this.Attachments.Count; i++)
            {
                MimeData mime;

                if (this.encryptMessage.IsChecked == false)
                {
                    mime = mailBuilder.AddAttachment(this.Attachments[i]);
                }
                else
                {
                    byte[] data = this.Attachments[i];
                    byte[] encrData = Encrypter.EncryptWithAesAndRsa(data,
                        this.EmailBox.UserKeyContainerName);
                    byte[] signedData = Encrypter.SignData(encrData, 
                        this.EmailBox.UserKeyContainerName);
                    mime = mailBuilder.AddAttachment(signedData);
                }

                mime.FileName = (string)this.attachmentsListBox.Items[i];
            }

            IMail mail = mailBuilder.Create();

            if (sendThread != null && sendThread.IsAlive)
            {
                sendThread.Abort();
                sendThread.Join();
            }

            sendThread = new Thread(() =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    Mouse.OverrideCursor = Cursors.AppStarting;
                });

                this.EmailBox.Smtp.SendMessage(mail);
                
                this.Dispatcher.Invoke(() =>
                {
                    this.Close();
                    Mouse.OverrideCursor = null;
                });
            });
            sendThread.Start();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (sendThread != null && sendThread.IsAlive)
            {
                sendThread.Abort();
                sendThread.Join();
            }
        }
    }
}
