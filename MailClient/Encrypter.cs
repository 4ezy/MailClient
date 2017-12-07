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

        //public static readonly byte[] DefaultKey = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        //    0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 };
        //public static readonly byte[] DefaultIV = new byte[] { 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27,
        //    0x28, 0x29, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35 };

        public static byte[] EncryptWithAesAndRsa(byte[] data, string keyContainerName)
        {
            AesCryptoServiceProvider aes = new AesCryptoServiceProvider() { Mode = CipherMode.CBC };
            ICryptoTransform ct = aes.CreateEncryptor();
            CspParameters cspp = new CspParameters { KeyContainerName = keyContainerName };
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(cspp);
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

        public static byte[] DecryptWithAesAndRsa(byte[] data, string keyContainerName)
        {
            CspParameters cspp = new CspParameters { KeyContainerName = keyContainerName };
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(cspp);
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

        public static byte[] EncryptWithAesManaged(byte[] data, byte[] key, byte[] iv)
        {
            AesManaged aes = new AesManaged
            {
                Key = key,
                IV = iv,
                Mode = CipherMode.CBC
            };
            ICryptoTransform ct = aes.CreateEncryptor();
            byte[] encData;

            using (MemoryStream ms = new MemoryStream()) 
            {
                using (CryptoStream cs = new CryptoStream(ms, ct, CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                }

                encData = ms.ToArray();
            }

            return encData;
        }

        public static byte[] DecryptWithAesManaged(byte[] data, byte[] key, byte[] iv)
        {
            AesManaged aes = new AesManaged()
            {
                Key = key,
                IV = iv,
                Mode = CipherMode.CBC
            };
            ICryptoTransform ct = aes.CreateDecryptor();
            byte[] decData = new byte[data.Length];

            using (MemoryStream ms = new MemoryStream(data))
            {
                using (CryptoStream cs = new CryptoStream(ms, ct, CryptoStreamMode.Read))
                {
                    cs.Read(decData, 0, decData.Length);
                }
            }

            return decData;
        }

        public static void EncryptFileWithAesManaged(string path, byte[] key, byte[] iv)
        {
            byte[] data = File.ReadAllBytes(path);
            byte[] encData = Encrypter.EncryptWithAesManaged(data, key, iv);
            File.WriteAllBytes(path, encData);
        }

        public static byte[] DecryptFileWithAesManaged(string path, byte[] key, byte[] iv)
        {
            byte[] decData = File.ReadAllBytes(path);
            byte[] data = Encrypter.DecryptWithAesManaged(decData, key, iv);
            return data;
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

        private static byte[] ComputeHashAndAddIt(byte[] data)
        {
            byte[] dataWithHash;
            byte[] hash = GetSha1Hash(data);
            byte[] hashLength = BitConverter.GetBytes(hash.Length);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(hashLength, 0, hashLength.Length);
                ms.Write(hash, 0, hash.Length);
                ms.Write(data, 0, data.Length);
                dataWithHash = ms.ToArray();
            }

            return dataWithHash;
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
                    dsa.ToXmlString(true));
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

        public static bool CheckSign(byte[] data, string keyContainerName)
        {
            bool checkResult;

            DSACryptoServiceProvider dsa = new DSACryptoServiceProvider();
            dsa.FromXmlString(File.ReadAllText(
                    MainWindow.UserDirectoryPath + keyContainerName + ".akey"));
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
                checkResult = dSADeformatter.VerifySignature(hash, signedHash);  // TODO: вот здесь косяк
            }

            return checkResult;
        }
    }
}
