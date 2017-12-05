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
    public partial class MailReadingWindow : Window
    {
        public IMail Message { get; private set; }

        public MailReadingWindow()
        {
            this.InitializeComponent();
        }

        public MailReadingWindow(IMail message)
        {
            this.InitializeComponent();
            this.Message = message;
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

                if (this.Message.Rtf == String.Empty)
                    this.textRichTextBox.AppendText(this.Message.Text);
                else
                    this.textRichTextBox.AppendText(this.Message.Rtf);

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
    }
}
