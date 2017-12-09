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
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
            else
            {
                MessageBox.Show("Выберите приложение, что удалить его!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.encryptMessage.IsChecked == true &&
                this.EmailBox.XmlStringChipherKeyContainerName is null)
            {
                MessageBox.Show("Для шифрования сообщения следует импортировать открытый ключ!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (this.toTextBox.Text is null)
            {
                MessageBox.Show("Требуется ввести имя получателя.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MailBuilder mailBuilder = new MailBuilder();
            mailBuilder.From.Add(new MailBox(this.EmailBox.EmailAddress));

            try
            {
                mailBuilder.To.Add(new MailBox(this.toTextBox.Text.Trim(' ')));
            }
            catch (ArgumentException)
            {
                MessageBox.Show("Имя получателя написано в неверном формате.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            mailBuilder.Subject = this.subjectTextBox.Text.Trim(' ');

            if (encryptMessage.IsChecked == true)
            {
                byte[] data = this.GetBytesFromRichTextBoxText(this.textRichTextBox);
                byte[] encData = Encrypter.EncryptWithAesAndRsa(data,
                    this.EmailBox.XmlStringChipherKeyContainerName, true);
                byte[] signedData = Encrypter.SignData(encData,
                    this.EmailBox.UserKeyContainerName);
                mailBuilder.Rtf = Convert.ToBase64String(signedData);
            }
            else
            {
                mailBuilder.Rtf = this.GetTextFromRichTextBox(this.textRichTextBox);
            }

            for (int i = 0; i < this.Attachments.Count; i++)
            {
                if (encryptMessage.IsChecked == true)
                {
                    byte[] data = this.Attachments[i];
                    byte[] encData = Encrypter.EncryptWithAesAndRsa(data,
                        this.EmailBox.XmlStringChipherKeyContainerName, true);
                    byte[] signedData = Encrypter.SignData(encData,
                        this.EmailBox.UserKeyContainerName);

                    MimeData mime = mailBuilder.AddAttachment(signedData);
                    mime.FileName = (string)this.attachmentsListBox.Items[i];
                }
                else
                {
                    MimeData mime = mailBuilder.AddAttachment(this.Attachments[i]);
                    mime.FileName = (string)this.attachmentsListBox.Items[i];
                }
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
                    this.sendButton.IsEnabled = false;
                    Mouse.OverrideCursor = Cursors.AppStarting;
                });

                this.EmailBox.Smtp.SendMessage(mail);
                
                this.Dispatcher.Invoke(() =>
                {
                    this.Close();
                    this.sendButton.IsEnabled = true;
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

        private string GetTextFromRichTextBox(RichTextBox richTextBox)
        {
            TextRange documentTextRange = new TextRange(
                richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);

            using (FileStream fs = File.Create(MainWindow.UserDirectoryPath + "tmp.rtf"))
            {
                documentTextRange.Save(fs, DataFormats.Rtf);
            }

            string str = File.ReadAllText(MainWindow.UserDirectoryPath + "tmp.rtf");
            File.Delete(MainWindow.UserDirectoryPath + "tmp.rtf");
            return str;
        }

        private byte[] GetBytesFromRichTextBoxText(RichTextBox richTextBox)
        {
            TextRange documentTextRange = new TextRange(
                richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);

            using (FileStream fs = File.Create(MainWindow.UserDirectoryPath + "tmp.rtf"))
            {
                documentTextRange.Save(fs, DataFormats.Rtf);
            }

            byte[] bytes = File.ReadAllBytes(MainWindow.UserDirectoryPath + "tmp.rtf");
            File.Delete(MainWindow.UserDirectoryPath + "tmp.rtf");
            return bytes;
        }
    }
}
