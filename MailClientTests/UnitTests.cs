using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MailClient;

namespace MailClientTests
{
    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        public void ConnectionCheck()
        {
            EmailBox emailBox = new EmailBox("aksonov10@gmail.com", "Db64ce15345", "imap.gmail.com",
                993, "smtp.gmail.com", 465);

            Assert.AreEqual(true, emailBox.CheckConnection());
        }

        [TestMethod]
        public void EmailDownload()
        {
            EmailBox emailBox = new EmailBox("aksonov10@gmail.com", "Db64ce15345", "imap.gmail.com",
                993, "smtp.gmail.com", 465);
            emailBox.DownloadAllInbox();
            Assert.IsNotNull(emailBox.Inbox);
        }
    }
}
