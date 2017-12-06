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
    /// Логика взаимодействия для BasketMailReadingWindow.xaml
    /// </summary>
    public partial class BasketMailReadingWindow : Window
    {
        public IMail Message { get; private set; }

        public BasketMailReadingWindow()
        {
            InitializeComponent();
        }

        public BasketMailReadingWindow(IMail message)
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

                this.toTextBox.Text = toString;

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
    }
}
