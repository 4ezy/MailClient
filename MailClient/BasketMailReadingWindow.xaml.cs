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
    /// Логика взаимодействия для BasketMailReadingWindow.xaml
    /// </summary>
    public partial class BasketMailReadingWindow : Window
    {
        public IMail Message { get; private set; }
        public EmailBox EmailBox { get; private set; }

        public BasketMailReadingWindow()
        {
            InitializeComponent();
        }

        public BasketMailReadingWindow(IMail message, EmailBox emailBox)
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

                this.fromTextBox.Text = fromString.Trim(' ');

                IList<MailAddress> to = Message.To;

                string toString = string.Empty;

                for (int i = 0; i < to.Count; i++)
                {
                    toString += to[i].Name;

                    IList<MailBox> toAddresses = to[i].GetMailboxes();

                    for (int j = 0; j < toAddresses.Count; j++)
                    {
                        toString += " (" + toAddresses[j].Address;

                        if (j == toAddresses.Count - 1)
                            toString += ")";
                        else
                            toString += ", ";
                    }

                    if (i < to.Count - 1)
                        toString += ", ";
                }

                this.toTextBox.Text = toString.Trim(' ');

                this.dateTextBox.Text = this.Message.Date.ToString();

                this.subjectTextBox.Text = this.Message.Subject;

                if (this.Message.IsText)
                {
                    this.textRichTextBox.AppendText(this.Message.Text);
                }
                else if (this.Message.IsRtf)
                {
                    this.SetStringRtfToRichTextBox(this.Message.Rtf, this.textRichTextBox);
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

        private void SetStringRtfToRichTextBox(string message, RichTextBox richTextBox)
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

        private void SetByteRtfToRichTextBox(byte[] data, RichTextBox richTextBox)
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
            if (this.attachmentsListBox.SelectedIndex != -1)
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
                        byte[] signData = this.Message.Attachments[this.attachmentsListBox.SelectedIndex].Data; ;
                        byte[] decData = this.TryDecryptData(signData);
                        if (!decData.Equals(signData))
                            File.WriteAllBytes(sfd.FileName, decData);
                    }
                    else
                    {
                        File.WriteAllBytes(sfd.FileName,
                            this.Message.Attachments[this.attachmentsListBox.SelectedIndex].Data);
                    }
                }
            }
            else
            {
                MessageBox.Show("Для сохранения файла его требуется выбрать.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DecryptMessage_Click(object sender, RoutedEventArgs e)
        {
            if (decryptMessage.IsChecked == true)
            {
                if (this.EmailBox.XmlStringSignKeyContainerName is null)
                {
                    MessageBox.Show("Для проверки подписи сообщения" +
                        " следует импортировать открытый ключ!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (this.Message.IsText)
                {
                    MessageBox.Show("Шифрование обычного текста не поддерживается", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (this.Message.IsRtf)
                {
                    byte[] signData = Convert.FromBase64String(this.Message.Rtf);
                    byte[] decData = this.TryDecryptData(signData);
                    if (!decData.Equals(signData))
                        this.SetByteRtfToRichTextBox(decData, this.textRichTextBox);
                }
                else if (this.Message.IsHtml)
                {
                    MessageBox.Show("Шифрование HTML страниц не поддерживается", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                if (this.Message.IsText)
                {
                    this.textRichTextBox.AppendText(this.Message.Text);
                }
                else if (this.Message.IsRtf)
                {
                    this.SetStringRtfToRichTextBox(this.Message.Rtf, this.textRichTextBox);
                }
                else if (this.Message.IsHtml)
                {
                    this.textRichTextBox.AppendText(this.Message.GetTextFromHtml());
                }
            }
        }

        private byte[] TryDecryptData(byte[] data)
        {
            bool signTrue = false;
            try
            {
                signTrue = Encrypter.CheckSign(data, this.EmailBox.XmlStringSignKeyContainerName);
            }
            catch (Exception)
            {
                MessageBox.Show("Подпись данных повреждена или отсутствует!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return data;
            }

            if (!signTrue)
            {
                MessageBox.Show("Подпись файла не совпадает!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return data;
            }

            byte[] decData;

            try
            {
                byte[] encData = Encrypter.ReturnDataWithoutHash(data);
                decData = Encrypter.DecryptWithAesAndRsa(encData, this.EmailBox.UserKeyContainerName, false);
            }
            catch (Exception)
            {
                MessageBox.Show("Данные повреждены!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return data;
            }

            return decData;
        }
    }
}
