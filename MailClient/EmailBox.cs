using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Limilabs.Client.IMAP;
using Limilabs.Client.SMTP;
using Limilabs.Mail;
using Limilabs.Client;

namespace MailClient
{
    [Serializable]
    public class EmailBox : ICloneable
    {
        public string EmailAddress { get; set; }
        public string Password { get; set; }
        public string ImapServerAddress { get; set; }
        public int ImapPort { get; set; }
        public string SmtpServerAddress { get; set; }
        public int SmtpPort { get; set; }
        public List<IMail> Inbox { get; set; }

        public EmailBox() { }
        public EmailBox(string emailAddress, string password, string imapServerAddress,
            int imapPort, string smtpServerAddress, int smtpPort)
        {
            this.EmailAddress = emailAddress;
            this.Password = password;
            this.ImapServerAddress = imapServerAddress;
            this.ImapPort = imapPort;
            this.SmtpServerAddress = smtpServerAddress;
            this.SmtpPort = smtpPort;
        }

        public override string ToString()
        {
            return String.Format("[EmailAddress: {0}; Password: {1}; ImapServerAddress: {2};" +
                " ImapPort: {3}; SmtpServerAddress: {4}; SmtpPort: {5}]",
                this.EmailAddress, this.Password, this.ImapServerAddress,
                this.ImapPort, this.SmtpServerAddress, this.SmtpPort);
        }

        public override bool Equals(object obj)
        {
            bool equals = false;
            if (obj is EmailBox)
            {
                if (this.EmailAddress == ((EmailBox)obj).EmailAddress)
                    equals = true;
            }

            return equals;
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public bool CheckConnection()
        {
            bool isOk = true;
            using (Imap imap = new Imap())
            {
                try
                {
                    imap.ConnectSSL(ImapServerAddress, ImapPort);
                    imap.UseBestLogin(EmailAddress, Password);
                }
                catch (Exception)
                {
                    return isOk = false;
                }
            }

            using (Smtp smtp = new Smtp())
            {
                try
                {
                    smtp.ConnectSSL(SmtpServerAddress, SmtpPort);
                    smtp.UseBestLogin(EmailAddress, Password);
                }
                catch (Exception)
                {
                    return isOk = false;
                }
            }

            return isOk;
        }

        public void DownloadAllInbox()
        {
            List<IMail> inbox = new List<IMail>();
            using (Imap imap = new Imap())
            {
                imap.ConnectSSL(ImapServerAddress, ImapPort);
                imap.UseBestLogin(EmailAddress, Password);
                imap.SelectInbox();
                List<long> uidList = imap.Search(Flag.All);
                Inbox = new List<IMail>();

                for (int i = 0; i < 10; i++)
                {
                    byte[] eml = imap.GetMessageByUID(uidList[i]);

                    Inbox.Add(new MailBuilder().CreateFromEml(eml));
                }
            }
        }

        public object Clone()
        {
            return new EmailBox
            {
                EmailAddress = this.EmailAddress,
                Password = this.Password,
                ImapServerAddress = this.ImapServerAddress,
                ImapPort = this.ImapPort,
                SmtpServerAddress = this.SmtpServerAddress,
                SmtpPort = this.SmtpPort,
                Inbox = this.Inbox
            };
        }
    }
}
