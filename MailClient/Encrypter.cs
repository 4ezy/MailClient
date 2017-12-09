using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

namespace MailClient
{
    public static class Encrypter
    {
        private static readonly string defaultKeyContainerName = "FileKeyContainer";
        public static string DefaultKeyContainerName => defaultKeyContainerName;

        public static byte[] EncryptWithAesAndRsa(byte[] data, string keyContainerName, bool isXmlString)
        {
            AesCryptoServiceProvider aes = new AesCryptoServiceProvider() { Mode = CipherMode.CBC };
            ICryptoTransform ct = aes.CreateEncryptor();
            RSACryptoServiceProvider rsa;

            if (!isXmlString)
            {
                CspParameters cspp = new CspParameters { KeyContainerName = keyContainerName };
                rsa = new RSACryptoServiceProvider(cspp);
            }
            else
            {
                rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(keyContainerName);
            }

            byte[] keyEncrypted = rsa.Encrypt(aes.Key, false);
            byte[] keyLength = BitConverter.GetBytes(keyEncrypted.Length);
            byte[] ivLength = BitConverter.GetBytes(aes.IV.Length);
            byte[] dataLength = BitConverter.GetBytes(data.Length);
            byte[] encryptedData;

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Seek(0, SeekOrigin.Begin);
                ms.Write(keyLength, 0, 4);
                ms.Write(ivLength, 0, 4);
                ms.Write(dataLength, 0, 4);
                ms.Write(keyEncrypted, 0, keyEncrypted.Length);
                ms.Write(aes.IV, 0, aes.IV.Length);

                using (CryptoStream cs = new CryptoStream(ms, ct, CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                }

                encryptedData = ms.ToArray();
            }

            aes.Dispose();
            ct.Dispose();
            rsa.Dispose();
            return encryptedData;
        }

        public static byte[] DecryptWithAesAndRsa(byte[] data, string keyContainerName, bool isXmlString)
        {
            RSACryptoServiceProvider rsa;
            if (!isXmlString)
            {
                CspParameters cspp = new CspParameters { KeyContainerName = keyContainerName };
                rsa = new RSACryptoServiceProvider(cspp);
            }
            else
            {
                rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(keyContainerName);
            }

            byte[] decryptedData;

            using (MemoryStream ms = new MemoryStream(data))
            {
                byte[] keyLengthBuffer = new byte[4];
                byte[] ivLengthBuffer = new byte[4];
                byte[] dataLengthBuffer = new byte[4];

                ms.Seek(0, SeekOrigin.Begin);
                ms.Read(keyLengthBuffer, 0, 3);
                int keyLength = BitConverter.ToInt32(keyLengthBuffer, 0);

                ms.Seek(4, SeekOrigin.Begin);
                ms.Read(ivLengthBuffer, 0, 3);
                int ivLength = BitConverter.ToInt32(ivLengthBuffer, 0);

                ms.Seek(8, SeekOrigin.Begin);
                ms.Read(dataLengthBuffer, 0, 3);
                int decryptedDataLength = BitConverter.ToInt32(dataLengthBuffer, 0);

                byte[] keyEncrypted = new byte[keyLength];
                byte[] iv = new byte[ivLength];
                decryptedData = new byte[decryptedDataLength];

                ms.Seek(12, SeekOrigin.Begin);
                ms.Read(keyEncrypted, 0, keyLength);
                ms.Seek(12 + keyLength, SeekOrigin.Begin);
                ms.Read(iv, 0, ivLength);

                int startEncryptedData = keyLength + ivLength + 12;
                int encryptedDataLength = (int)ms.Length - startEncryptedData;

                byte[] keyDecrypted = rsa.Decrypt(keyEncrypted, false);
                AesCryptoServiceProvider aes = new AesCryptoServiceProvider()
                {
                    Key = keyDecrypted,
                    IV = iv,
                    Mode = CipherMode.CBC
                };
                ICryptoTransform ct = aes.CreateDecryptor();
                ms.Seek(startEncryptedData, SeekOrigin.Begin);

                using (CryptoStream cs = new CryptoStream(ms, ct, CryptoStreamMode.Read))
                {
                    cs.Read(decryptedData, 0, decryptedData.Length);
                }

                aes.Dispose();
                ct.Dispose();
            }

            rsa.Dispose();
            return decryptedData;
        }

        private static byte[] GetSha1Hash(byte[] data)
        {
            byte[] signedData;
            using (SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider())
            {
                signedData = sha1.ComputeHash(data);
            }
            return signedData;
        }

        public static byte[] SignData(byte[] data, string keyContainerName)
        {
            byte[] signedData;
            byte[] hash = GetSha1Hash(data);

            DSACryptoServiceProvider dsa = new DSACryptoServiceProvider();

            if (File.Exists(MainWindow.UserDirectoryPath + keyContainerName + ".akey"))
            {
                dsa.FromXmlString(File.ReadAllText(
                    MainWindow.UserDirectoryPath + keyContainerName + ".akey"));
            }
            else
            {
                File.WriteAllText(MainWindow.UserDirectoryPath + keyContainerName + ".akey",
                    dsa.ToXmlString(true)); // TODO: шифровать это
            }

            DSASignatureFormatter dsaFormatter = new DSASignatureFormatter(dsa);
            dsaFormatter.SetHashAlgorithm("SHA1");
            byte[] signedHash = dsaFormatter.CreateSignature(hash);
            byte[] signedHashLength = BitConverter.GetBytes(signedHash.Length);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(signedHashLength, 0, signedHashLength.Length);
                ms.Write(signedHash, 0, signedHash.Length);
                ms.Write(data, 0, data.Length);
                signedData = ms.ToArray();
            }

            return signedData;
        }

        public static byte[] ReturnDataWithoutHash(byte[] data)
        {
            byte[] dataWithoutSign;
            using (MemoryStream ms = new MemoryStream(data))
            {
                byte[] signedHashLength = new byte[4];
                ms.Seek(0, SeekOrigin.Begin);
                ms.Read(signedHashLength, 0, 3);
                ms.Seek(4 + BitConverter.ToInt32(signedHashLength, 0), SeekOrigin.Begin);
                int dataStartPos = 4 + BitConverter.ToInt32(signedHashLength, 0);
                int dataEndPos = (int)ms.Length - dataStartPos;

                ms.Seek(dataStartPos, SeekOrigin.Begin);
                dataWithoutSign = new byte[dataEndPos];
                ms.Read(dataWithoutSign, 0, dataEndPos);
            }

            return dataWithoutSign;
        }

        public static bool CheckSign(byte[] data, string xmlStringPubKey)
        {
            bool checkResult;

            DSACryptoServiceProvider dsa = new DSACryptoServiceProvider();
            
            dsa.FromXmlString(xmlStringPubKey);

            DSASignatureDeformatter dSADeformatter = new DSASignatureDeformatter(dsa);
            dSADeformatter.SetHashAlgorithm("SHA1");

            using (MemoryStream ms = new MemoryStream(data))
            {
                byte[] signedHashLength = new byte[4];
                ms.Seek(0, SeekOrigin.Begin);
                ms.Read(signedHashLength, 0, 3);
                byte[] signedHash = new byte[BitConverter.ToInt32(signedHashLength, 0)];
                ms.Seek(4, SeekOrigin.Begin);
                ms.Read(signedHash, 0, signedHash.Length);

                byte[] dat = ReturnDataWithoutHash(data);
                byte[] hash = GetSha1Hash(dat);

                checkResult = dSADeformatter.VerifySignature(hash, signedHash);
            }

            return checkResult;
        }
    }
}
