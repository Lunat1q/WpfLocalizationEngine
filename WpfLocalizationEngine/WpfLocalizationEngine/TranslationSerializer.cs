using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace WpfLocalizationEngine
{
    internal static class TranslationSerializer
    {
        public static void CryptData<T>(this T data, string path, byte[] key)
        {
            try
            {
                if (data == null)
                    throw new ArgumentNullException();


                using (var memoryStream = new MemoryStream())
                {
                    SerializeToStream(memoryStream, data);
                    var eKey = GetProper16Key(key);
                    var iv = GetProper16Key(key, true);
                    File.WriteAllBytes(path, EncryptBytes(memoryStream.ToArray(), eKey, iv));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[TRANSLATION SERIALIZER FAILURE] " + ex.Message + " trace: " + ex.StackTrace);
            }
        }

        public static T DeserializeFromStream<T>(Stream stream)
        {
            var serializer = new JsonSerializer { Formatting = Formatting.Indented };
            serializer.Converters.Add(new StringEnumConverter { CamelCaseText = true });

            using (var sr = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(sr))
            {
                return serializer.Deserialize<T>(jsonTextReader);
            }
        }

        public static void SerializeToStream(Stream stream, object data)
        {
            var serializer = new JsonSerializer { Formatting = Formatting.Indented };
            serializer.Converters.Add(new StringEnumConverter { CamelCaseText = true });

            using (var sw = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            using (var jsonTextReader = new JsonTextWriter(sw))
            {
                serializer.Serialize(jsonTextReader, data);
            }
        }

        public static T DecryptData<T>(string path, byte[] key)
        {
            if (!File.Exists(path))
                return default(T);

            try
            {
                var eKey = GetProper16Key(key);
                var iv = GetProper16Key(key, true);
                var fileArray = File.ReadAllBytes(path);
                using (var memoryStream = new MemoryStream(DecryptBytes(fileArray, eKey, iv)))
                {
                    var obj = DeserializeFromStream<T>(memoryStream);
                    return obj;
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("[TRANSLATION DESERIALIZER FAILURE] " + ex.Message + " trace: " + ex.StackTrace);
                return default(T);
            }
        }

        #region CryptingZone
        private static byte[] GetProper16Key(byte[] key, bool reverse = false)
        {
            if (key.Length >= 16)
                return reverse ? key.Take(16).Reverse().ToArray() : key.Take(16).ToArray();
            else
            {
                var key16 = new byte[16];
                var offset = 0;
                for (var i = 0; i < 16; i++)
                {
                    key16[i] = key[i - offset];
                    if (i - offset == key.Length - 1)
                    {
                        offset += key.Length;
                    }
                }
                return reverse ? key16.Reverse().ToArray() : key16;
            }
        }

        private static byte[] EncryptBytes(byte[] toEncrypt, byte[] key, byte[] iv)
        {
            using (var rmCrypto = new RijndaelManaged())
            using (var ms = new MemoryStream())
            using (var encryptor = rmCrypto.CreateEncryptor(key, iv))
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(toEncrypt, 0, toEncrypt.Length);
                cs.FlushFinalBlock();
                return ms.ToArray();
            }
        }
        private static byte[] DecryptBytes(byte[] toDecrypt, byte[] key, byte[] iv)
        {
            using (var rmCrypto = new RijndaelManaged())
            using (var ms = new MemoryStream())
            using (var decryptor = rmCrypto.CreateDecryptor(key, iv))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
            {
                cs.Write(toDecrypt, 0, toDecrypt.Length);
                cs.FlushFinalBlock();
                return ms.ToArray();
            }
        }
        #endregion

        public static bool SerializeDataJson<T>(this T data, string path)
        {
            try
            {
                var p = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(p))
                    Directory.CreateDirectory(p);
                var js = new JsonSerializer();
                js.Converters.Add(new JavaScriptDateTimeConverter());
                js.NullValueHandling = NullValueHandling.Ignore;
                js.Formatting = Formatting.Indented;
                js.TypeNameHandling = TypeNameHandling.Objects;

                using (StreamWriter sw = new StreamWriter(path))
                using (JsonWriter writer = new JsonTextWriter(sw))
                    js.Serialize(writer, data);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static T DeserializeDataJson<T>(string path)
        {
            if (!File.Exists(path))
                return default(T);
            try
            {
                using (var file = File.OpenText(path))
                {
                    var serializer = new JsonSerializer { TypeNameHandling = TypeNameHandling.Objects };
                    serializer.Converters.Add(new JavaScriptDateTimeConverter());
                    var data = (T)serializer.Deserialize(file, typeof(T));
                    return data;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return default(T);
            }
        }
    }
}