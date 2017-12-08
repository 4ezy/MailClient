using Limilabs.Mail;
using Limilabs.Mail.Headers;
using Limilabs.Mail.MIME;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// Логика взаимодействия для MailReadingWindow.xaml
    /// </summary>
    public partial class InboxMailReadingWindow : Window
    {
        public IMail Message { get; private set; }
        public EmailBox EmailBox { get; private set; }

        public InboxMailReadingWindow()
        {
            this.InitializeComponent();
        }

        public InboxMailReadingWindow(IMail message, EmailBox emailBox)
        {
            this.InitializeComponent();
            this.Message = message;
            this.EmailBox = emailBox;
            this.ShowMessageInWindow();
        }

        private void ShowMessageInWindow()
        {
            try
            {
                IList<MailBox> fromAddresses = Message.From;

                string fromString = string.Empty;

                for (int i = 0; i < fromAddresses.Count; i++)
                {
                    fromString += fromAddresses[i].Name + " (" + fromAddresses[i].Address + ")";

                    if (i < fromAddresses.Count - 1)
                        fromString += ", ";
                }

                this.fromTextBox.Text = fromString;

                this.dateTextBox.Text = this.Message.Date.ToString();

                this.subjectTextBox.Text = this.Message.Subject;

                if (this.Message.IsText && !this.Message.IsRtf)
                {
                    this.textRichTextBox.AppendText(this.Message.Text);
                }
                else if (this.Message.IsRtf)
                {
                    this.SetRtfTextToRichTextBox(this.Message.Rtf, this.textRichTextBox);
                }
                else if (this.Message.IsHtml)
                {
                    this.textRichTextBox.AppendText(this.Message.GetTextFromHtml());
                } 

                foreach (MimeData item in this.Message.Attachments)
                {
                    this.attachmentsListBox.Items.Add(item.FileName);
                }

                if (this.attachmentsListBox.HasItems)
                    this.saveAttachmentsButton.IsEnabled = true;
                else
                    this.saveAttachmentsButton.IsEnabled = false;

            }
            catch (NullReferenceException)
            {
                throw;
            }
        }

        private void SetRtfTextToRichTextBox(string message, RichTextBox richTextBox)
        {
            File.WriteAllText(MainWindow.UserDirectoryPath + "tmp.rtf", message);

            TextRange tr = new TextRange(richTextBox.Document.ContentStart,
                richTextBox.Document.ContentEnd);

            using (FileStream fs = File.Open(MainWindow.UserDirectoryPath + "tmp.rtf", FileMode.Open))
            {
                tr.Load(fs, DataFormats.Rtf);
            }

            File.Delete(MainWindow.UserDirectoryPath + "tmp.rtf");
        }

        private void SetRtfTextToRichTextBox(byte[] data, RichTextBox richTextBox)
        {
            File.WriteAllBytes(MainWindow.UserDirectoryPath + "tmp.rtf", data);

            TextRange tr = new TextRange(richTextBox.Document.ContentStart,
                richTextBox.Document.ContentEnd);

            using (FileStream fs = File.Open(MainWindow.UserDirectoryPath + "tmp.rtf", FileMode.Open))
            {
                tr.Load(fs, DataFormats.Rtf);
            }

            File.Delete(MainWindow.UserDirectoryPath + "tmp.rtf");
        }

        private void SaveAttachmentsButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog()
            {
                Title = "Сохранить файл",
                FileName = (string)this.attachmentsListBox.SelectedItem,
                Filter = "Все файлы (*.*)|*.*"
            };
            if (sfd.ShowDialog() == true)
            {
                if (this.decryptMessage.IsChecked == true)
                {
                    byte[] encDataWithHash = this.Message.Attachments[this.attachmentsListBox.SelectedIndex].Data; ;
                    bool signTrue = false;
                    try
                    {
                        signTrue = Encrypter.CheckSign(encDataWithHash, this.EmailBox.UserKeyContainerName);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Подпись файла отсутствует!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (!signTrue)
                    {
                        MessageBox.Show("Подпись файла не совпадает!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    byte[] decData;

                    try
                    {
                        byte[] encData = Encrypter.ReturnDataWithoutHash(encDataWithHash);
                        decData = Encrypter.DecryptWithAesAndRsa(encData, this.EmailBox.UserKeyContainerName);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Данные повреждены!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    File.WriteAllBytes(sfd.FileName, decData);
                }
                else
                {
                    File.WriteAllBytes(sfd.FileName, 
                        this.Message.Attachments[this.attachmentsListBox.SelectedIndex].Data);
                }
            }
        }

        private void DecryptMessage_Click(object sender, RoutedEventArgs e)
        {
            byte[] signData = Convert.FromBase64String(this.Message.Rtf);

            bool signTrue = false;
            try
            {
                signTrue = Encrypter.CheckSign(signData, this.EmailBox.UserKeyContainerName);
            }
            catch (Exception)
            {
                MessageBox.Show("Подпись файла отсутствует!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!signTrue)
            {
                MessageBox.Show("Подпись файла не совпадает!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            byte[] decData;

            try
            {
                byte[] encData = Encrypter.ReturnDataWithoutHash(signData);
                decData = Encrypter.DecryptWithAesAndRsa(encData, this.EmailBox.UserKeyContainerName);
            }
            catch (Exception)
            {
                MessageBox.Show("Данные повреждены!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.SetRtfTextToRichTextBox(decData, this.textRichTextBox);
        }
    }
}
