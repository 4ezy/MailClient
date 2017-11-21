using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace MailClient
{
    public static class BinarySerializer
    {
        public static void Serialize<T>(T obj, string path)
        {
            try
            {
                using (Stream stream = new FileStream(path, FileMode.Create,
                    FileAccess.Write, FileShare.None))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    binaryFormatter.Serialize(stream, obj);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static byte[] Serialize<T>(T obj)
        {
            byte[] serData;

            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    binaryFormatter.Serialize(ms, obj);
                    serData = ms.GetBuffer();
                }
            }
            catch (Exception)
            {
                throw;
            }

            return serData;
        }

        public static T Deserialize<T>(string path)
        {
            try
            {
                using (Stream stream = new FileStream(path, FileMode.Open,
                        FileAccess.Read, FileShare.None))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    return (T)binaryFormatter.Deserialize(stream);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static T Deserialize<T>(byte[] data)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    return (T)binaryFormatter.Deserialize(ms);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
