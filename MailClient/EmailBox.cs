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

        [NonSerialized]
        private Imap imap;

        public Imap Imap
        {
            get { return imap; }
            set { imap = value; }
        }

        [NonSerialized]
        private Smtp smtp;

        public Smtp Smtp
        {
            get { return smtp; }
            set { smtp = value; }
        }

        [NonSerialized]
        private List<Envelope> inbox;
        public List<Envelope> Inbox
        {
            get { return inbox; }
            set { inbox = value; }
        }

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

        public bool Connect()
        {
            bool isOk = true;

            try
            {
                this.imap = new Imap();
                this.imap.ConnectSSL(ImapServerAddress, ImapPort);
                this.imap.UseBestLogin(EmailAddress, Password);
                this.imap.SelectInbox();
                this.inbox = new List<Envelope>();
            }
            catch (Exception)
            {
                return isOk = false;
            }

            try
            {
                this.smtp = new Smtp();
                this.smtp.ConnectSSL(SmtpServerAddress, SmtpPort);
                this.smtp.UseBestLogin(EmailAddress, Password);
            }
            catch (Exception)
            {
                return isOk = false;
            }

            return isOk;
        }

        public void ChangeFolder(MessagesType messagesType)
        {
            List<FolderInfo> list = imap.GetFolders();
            FolderInfo folder = null;

            if (messagesType == MessagesType.Inbox)
            {
                folder = (from f in list
                          where f.ShortName == "Входящие"
                          select f).First();
            }
            else if (messagesType == MessagesType.Sent)
            {
                folder = (from f in list
                          where f.ShortName == "Отправленные"
                          select f).First();
            }
            else if (messagesType == MessagesType.Drafts)
            {
                folder = (from f in list
                          where f.ShortName == "Черновики"
                          select f).First();
            }
            else if (messagesType == MessagesType.Basket)
            {
                folder = (from f in list
                          where f.ShortName == "Корзина"
                          select f).First();
            }

            try
            {
                imap.Select(folder);
            }
            catch (NullReferenceException)
            {
                return;
            }
        }

        public void DownloadEnvelopes(int offset, int maxMessagesCount, 
            MessagesBeginningFrom beginningFrom, Action<string> subjectAddAction)
        {
            List<long> uidList = imap.Search(Flag.All);
            this.Inbox = new List<Envelope>();

            if (beginningFrom == MessagesBeginningFrom.New)
                uidList.Reverse();

            for (int i = offset; i < uidList.Count && maxMessagesCount > 0; i++)
            {
                Envelope envelope = null;
                try
                {
                    envelope = imap.GetEnvelopeByUID(uidList[i]);
                }
                catch (ServerException)
                {
                    throw;
                }

                this.Inbox.Add(envelope);
                subjectAddAction.Invoke(envelope.Subject);
                maxMessagesCount--;
            }
        }
    }
}

public enum MessagesBeginningFrom
{
    Old,
    New
}

public enum MessagesType
{
    Inbox,
    Sent,
    Drafts,
    Basket
}