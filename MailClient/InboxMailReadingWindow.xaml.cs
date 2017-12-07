using Limilabs.Mail;
using Limilabs.Mail.Headers;
using Limilabs.Mail.MIME;
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

                if (this.Message.IsText)
                    this.textRichTextBox.AppendText(this.Message.Text);
                else if (this.Message.IsRtf)
                    this.textRichTextBox.AppendText(this.Message.Rtf);
                else if (this.Message.IsHtml)
                    this.textRichTextBox.AppendText("Это письмо содержит html. К сожалению, его отображение не поддерживается.");

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

        private void DecryptMessage_Checked(object sender, RoutedEventArgs e)
        {
            char[] rtfText = this.Message.Rtf.ToCharArray();
            //Encoding encoding = Encoding.GetEncoding(Encoding.UTF8.CodePage);

            List<byte> data = new List<byte>();

            for (int i = 0; i < rtfText.Length; i++)
            {
                byte[] charBytes = BitConverter.GetBytes(rtfText[i]);
                data.AddRange(charBytes);
            }

            if (Encrypter.CheckSign(data.ToArray(),
                this.EmailBox.UserKeyContainerName))
                MessageBox.Show("Ebat' ti molodec");
        }
    }
}
