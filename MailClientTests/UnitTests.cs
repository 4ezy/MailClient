using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MailClient;
using System.IO;
using System.Collections.Generic;
using System.Text;

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

            Assert.AreEqual(true, emailBox.ConnectFull());
        }

        [TestMethod]
        public void EmailDownload()
        {
            EmailBox emailBox = new EmailBox("aksonov10@gmail.com", "Db64ce15345", "imap.gmail.com",
                993, "smtp.gmail.com", 465);
            //emailBox.DownloadInboxMessages(0, 5);
            Assert.IsNotNull(emailBox.Inbox);
        }

        [TestMethod]
        public void EncryptAndDecryptFile()
        {
            byte[] data = File.ReadAllBytes(@"C:\Users\Sergey\Desktop\f.txt");
            byte[] encData = Encrypter.EncryptWithAesAndRsa(data, Encrypter.DefaultKeyContainerName);
            byte[] decData = Encrypter.DecryptWithAesAndRsa(encData, Encrypter.DefaultKeyContainerName);

            Assert.AreEqual(data.Length, decData.Length);

            for (int i = 0; i < data.Length; i++)
            {
                Assert.AreEqual(data[i], decData[i]);
            }
        }

        [TestMethod]
        public void ListCapacityTest()
        {
            int[] list = new int[5];
            list[4] = 4;
        }

        [TestMethod]
        public void SignCheck()
        {
            string data = "123";
            Encoding encoding = Encoding.GetEncoding(Encoding.UTF8.CodePage);
            byte[] encrData = Encrypter.EncryptWithAesAndRsa(encoding.GetBytes(data),
                "vasya");
            byte[] signedData = Encrypter.SignData(encrData,
                    "vasya");
            string encString = String.Empty;

            for (int i = 0; i < signedData.Length; i += 2)
            {
                encString += BitConverter.ToChar(signedData, i);
            }

            char[] rtfText = encString.ToCharArray();
            List<byte> dat = new List<byte>();

            for (int i = 0; i < rtfText.Length; i++)
            {
                byte[] charBytes = BitConverter.GetBytes(rtfText[i]);
                dat.AddRange(charBytes);
            }

            bool actual = Encrypter.CheckSign(dat.ToArray(),
                "vasya");
            bool expected = true;

            Assert.AreEqual(expected, actual);
        }
    }
}
