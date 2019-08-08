using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Security.Cryptography;
using System.IO;

using ThirdParty.Json.LitJson;

using log4net;

namespace AWSDeploymentCryptoUtility
{
    public static class CryptoUtil
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(CryptoUtil));

        public static string DecryptBytes(byte[] ciphertext, byte[] key, byte[] iv)
        {
            if (LOGGER.IsDebugEnabled)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var b in key)
                {
                    if (sb.Length > 0)
                        sb.Append(", ");
                    sb.Append(b);
                }
                LOGGER.DebugFormat("Cypher Key: {0}", sb.ToString());
            }

            MemoryStream inputStream  = null;
            CryptoStream cryptoStream = null;
            StreamReader outputStream = null;

            string plaintext = null;
            Aes    cipher    = null;

            try
            {
                cipher = Aes.Create();
                cipher.Mode = CipherMode.CBC;
                cipher.Padding = PaddingMode.PKCS7;
                cipher.KeySize = 256;
                cipher.IV = iv;
                cipher.Key = key;

                ICryptoTransform decryptor = cipher.CreateDecryptor();

                inputStream = new MemoryStream(ciphertext);
                cryptoStream = new CryptoStream(inputStream, decryptor, CryptoStreamMode.Read);
                outputStream = new StreamReader(cryptoStream);

                plaintext = outputStream.ReadToEnd();
                LOGGER.DebugFormat("Decrypted text: {0}", plaintext);
            }
            catch (Exception e)
            {
                LOGGER.Debug("Exception decrypting", e);
                throw;
            }
            finally
            {
                if (outputStream != null)
                    outputStream.Close();
                if (cryptoStream != null)
                    cryptoStream.Close();
                if (inputStream != null)
                    inputStream.Close();

                if (cipher != null)
                    cipher.Clear();
            }

            return plaintext;
        }

        public static byte[] EncryptString(string plaintext, byte[] key, byte[] iv)
        {
            MemoryStream outputStream = null;
            CryptoStream cryptoStream = null;
            StreamWriter inputStream = null;

            Aes cipher = null;

            try
            {
                cipher = Aes.Create();
                cipher.Mode = CipherMode.CBC;
                cipher.Padding = PaddingMode.PKCS7;
                cipher.KeySize = 256;
                cipher.Key = key;
                cipher.IV = iv;

                ICryptoTransform encryptor = cipher.CreateEncryptor();

                outputStream = new MemoryStream();
                cryptoStream = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write);
                inputStream = new StreamWriter(cryptoStream);

                inputStream.Write(plaintext);
            }
            finally
            {
                if (inputStream != null)
                    inputStream.Close();
                if (cryptoStream != null)
                    cryptoStream.Close();
                if (outputStream != null)
                    outputStream.Close();

                if (cipher != null)
                    cipher.Clear();
            }

            return outputStream.ToArray();
        }

        public static string DecryptFromBase64EncodedString(string cipherText, byte[] key, byte[] iv)
        {
            return DecryptBytes(Convert.FromBase64String(cipherText), key, iv);
        }

        public static string EncryptToBase64EncodedString(string plaintext, byte[] key, byte[] iv)
        {           
            return Convert.ToBase64String(EncryptString(plaintext, key, iv));
        }

        public static string Timestamp()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        public static byte[] ConstructKeyFromEC2Metadata(string instanceId, string reservationId, string timestamp)
        {
            string keymatter = String.Format("{0}{1}{2}", instanceId, reservationId, timestamp);
            SHA256 hash = new SHA256Managed();
            System.Text.Encoding encoding = new System.Text.UTF8Encoding();
            return hash.ComputeHash(encoding.GetBytes(keymatter));
        }

        private static byte[] ConstructKeyWithIntegrator(EncryptionKeyTimestampIntegrator keymatter, string timestamp)
        {
            string toHash = keymatter(timestamp);
            SHA256 hash = new SHA256Managed();
            System.Text.Encoding encoding = new System.Text.UTF8Encoding();
            return hash.ComputeHash(encoding.GetBytes(toHash));
        }

        public const string
            JSON_KEY_IV        = "iv",
            JSON_KEY_TIMESTAMP = "timestamp",
            JSON_KEY_PAYLOAD   = "payload";

        // Use to combine the timestamp with other data which will be hashed to form the crypto key
        public delegate string EncryptionKeyTimestampIntegrator(string timestamp);

        public static JsonData DecryptRequest(string json, EncryptionKeyTimestampIntegrator keymatter)
        {
            string ivStr;
            string payload;
            string timestamp;
            byte[] iv;
            byte[] key;

            LOGGER.DebugFormat("JSON to JsonData: {0}", json);
            JsonData jData = JsonMapper.ToObject(json);

            if (jData[JSON_KEY_IV] != null && jData[JSON_KEY_IV].IsString)
            {
                ivStr = (string)jData[JSON_KEY_IV];
                iv = Convert.FromBase64String(ivStr);
            }
            else
                throw new ArgumentException("Invalid initial vector");

            if (jData[JSON_KEY_TIMESTAMP] != null && jData[JSON_KEY_TIMESTAMP].IsString)
            {
                timestamp = (string)jData[JSON_KEY_TIMESTAMP];
                key = ConstructKeyWithIntegrator(keymatter, timestamp);
            }
            else
                throw new ArgumentException("Invalid timestamp");

            if (jData[JSON_KEY_PAYLOAD] != null & jData[JSON_KEY_PAYLOAD].IsString)
            {
                payload = DecryptFromBase64EncodedString((string)jData[JSON_KEY_PAYLOAD], key, iv);
            }
            else
                throw new ArgumentException("Invalid payload");

            jData[JSON_KEY_PAYLOAD] = payload;

            return jData;
        }

        public static string EncryptResponse(string payload, byte[] iv, string timestamp, EncryptionKeyTimestampIntegrator keymatter)
        {
            JsonData jData = new JsonData();
            jData[JSON_KEY_IV] = Convert.ToBase64String(iv);
            jData[JSON_KEY_TIMESTAMP] = timestamp;

            jData[JSON_KEY_PAYLOAD] =
                EncryptToBase64EncodedString(payload, ConstructKeyWithIntegrator(keymatter, timestamp), iv);

            return JsonMapper.ToJson(jData);
        }
    }
}
