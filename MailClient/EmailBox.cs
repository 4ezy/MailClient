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
        private List<IMail> inbox;
        public List<IMail> Inbox
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
                this.inbox = new List<IMail>();
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

        public void DownloadInboxMessages(int offset, int maxMessagesCount, MessagesType messagesType,
            MessagesBeginningFrom beginningFrom, Action<string> subjectAddAction)
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

            try
            {
                imap.Select(folder);
            }
            catch (NullReferenceException)
            {
                return;
            }

            List<long> uidList = imap.Search(Flag.All);
            this.Inbox = new List<IMail>();

            if (beginningFrom == MessagesBeginningFrom.New)
                uidList.Reverse();

            for (int i = offset; i < uidList.Count && maxMessagesCount > 0; i++)
            {
                byte[] eml;
                try
                {
                    eml = imap.GetMessageByUID(uidList[i]);
                }
                catch (ServerException)
                {
                    throw;
                }
                IMail message = new MailBuilder().CreateFromEml(eml);
                this.Inbox.Add(message);
                subjectAddAction.Invoke(message.Subject);
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