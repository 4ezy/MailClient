using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

namespace MailClient
{
    public class Encrypter
    {
        public static readonly byte[] DefaultKey = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 };
        public static readonly byte[] DefaultIV = new byte[] { 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27,
            0x28, 0x29, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35 };

        public static byte[] AesEncrypt(byte[] data, byte[] key, byte[] iv)
        {
            AesManaged aes = new AesManaged
            {
                Key = key,
                IV = iv,
                Mode = CipherMode.CBC
            };
            ICryptoTransform ct = aes.CreateEncryptor();
            byte[] encData;
            //byte[] keyLength = BitConverter.GetBytes(aes.Key.Length);
            //byte[] ivLength = BitConverter.GetBytes(aes.IV.Length); 

            using (MemoryStream ms = new MemoryStream()) 
            {
                //ms.Write(keyLength, 0, keyLength.Length);
                //ms.Write(ivLength, 0, ivLength.Length);
                //ms.Write(aes.Key, 0, aes.Key.Length);
                //ms.Write(aes.IV, 0, aes.IV.Length);

                using (CryptoStream cs = new CryptoStream(ms, ct, CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                }

                encData = ms.ToArray();
            }

            return encData;
        }

        public static byte[] AesDecrypt(byte[] data, byte[] key, byte[] iv)
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

        public static void AesEncryptFile(string path, byte[] key, byte[] iv)
        {
            byte[] data = File.ReadAllBytes(path);
            byte[] encData = Encrypter.AesEncrypt(data, key, iv);
            File.WriteAllBytes(path, encData);
        }

        public static byte[] AesDecryptFile(string path, byte[] key, byte[] iv)
        {
            byte[] decData = File.ReadAllBytes(path);
            byte[] data = Encrypter.AesDecrypt(decData, key, iv);
            return data;
        }
    }
}
