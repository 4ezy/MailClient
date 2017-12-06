using Limilabs.Client.IMAP;
using Limilabs.Mail;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
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
    /// Логика взаимодействия для SendMailWindow.xaml
    /// </summary>
    public partial class SendMailWindow : Window
    {
        public Imap Imap { get; private set; }
        private List<byte[]> Attachments { get; set; }

        public SendMailWindow()
        {
            this.InitializeComponent();
        }

        public SendMailWindow(Imap userImap)
        {
            this.InitializeComponent();
            this.Imap = userImap;
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
                    this.Attachments.Add(File.ReadAllBytes(ofd.FileNames[i]));
                    this.attachmentsListBox.Items.Add(ofd.SafeFileNames[i]);
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
    }
}
