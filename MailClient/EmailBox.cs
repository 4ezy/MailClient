using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Limilabs.Client.IMAP;
using Limilabs.Client.SMTP;
using Limilabs.Mail;
using Limilabs.Client;
using Limilabs.Mail.Headers;
using System.Security.Cryptography;

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
        private string userKeyContainerName;
        public string UserKeyContainerName { get => userKeyContainerName; }

        [NonSerialized]
        private string xmlStringChipherKeyContainerName;
        public string XmlStringChipherKeyContainerName { get => xmlStringChipherKeyContainerName; set => xmlStringChipherKeyContainerName = value; }

        [NonSerialized]
        private string xmlStringSignKeyContainerName;
        public string XmlStringSignKeyContainerName { get => xmlStringSignKeyContainerName; set => xmlStringSignKeyContainerName = value; }

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

        public bool ConnectImap()
        {
            bool isOk = false;

            try
            {
                this.imap = new Imap();
                this.imap.ConnectSSL(ImapServerAddress, ImapPort);
                this.imap.UseBestLogin(EmailAddress, Password);
                this.inbox = new List<Envelope>();
                isOk = true;
            }
            catch (Exception)
            {
                isOk = false;
            }

            if (isOk)
                this.userKeyContainerName = this.EmailAddress;

            return isOk;
        }

        public bool ConnectSmtp()
        {
            bool isOk = false;

            try
            {
                this.smtp = new Smtp();
                this.smtp.ConnectSSL(SmtpServerAddress, SmtpPort);
                this.smtp.UseBestLogin(EmailAddress, Password);
                isOk = true;
            }
            catch (Exception)
            {
                isOk = false;
            }

            if (isOk)
                this.userKeyContainerName = this.EmailAddress;

            return isOk;
        }

        public bool ConnectFull()
        {
            bool isOk = false;

            isOk = this.ConnectImap();

            isOk = this.ConnectSmtp();

            if (isOk)
                this.userKeyContainerName = this.EmailAddress;

            return isOk;
        }

        public void ChangeFolder(MessagesType messagesType)
        {
            List<FolderInfo> list = imap.GetFolders();
            FolderInfo folder = null;

            try
            {
                if (messagesType == MessagesType.Inbox)
                {
                    folder = (from f in list
                              where f.ShortName == "Входящие" || f.ShortName == "INBOX"
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
                              where f.ShortName == "Корзина" || f.ShortName == "Удаленные"
                              select f).First();
                }
            }
            catch (Exception)
            {
                folder = null;
            }

            try
            {
                imap.Select(folder);
            }
            catch (NullReferenceException)
            {
                throw new Exception("Папки с таким именем не существует.");
            }
        }

        public void UploadMessageToDrafts(IMail mail)
        {
            List<FolderInfo> list = imap.GetFolders();
            FolderInfo folder = null;

            try
            {
                folder = (from f in list
                          where f.ShortName == "Черновики"
                          select f).First();
            }
            catch (Exception)
            {
                folder = null;
            }

            try
            {
                imap.Select(folder);
            }
            catch (NullReferenceException)
            {
                throw new Exception("Папки с таким именем не существует.");
            }

            this.imap.UploadMessage(folder, mail);
        }

        public IMail DownloadMessage(long uid)
        {
            byte[] eml = imap.GetMessageByUID(uid);
            IMail mail = new MailBuilder().CreateFromEml(eml);
            return mail;
        }

        public void DownloadEnvelopes(int offset, int maxMessagesCount, 
            MessagesBeginningFrom beginningFrom, Action<string, long> subjectAddAction)
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

                IList<MailBox> fromAddresses = envelope.From;
                string fromString = string.Empty;

                for (int j = 0; j < fromAddresses.Count; j++)
                {
                    fromString += fromAddresses[j].Name + " (" + fromAddresses[j].Address + ")";

                    if (j < fromAddresses.Count - 1)
                        fromString += ", ";
                }

                subjectAddAction.Invoke(fromString + " | " + envelope.Subject + " | " + envelope.Date.ToString(),
                    envelope.UID.Value);
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