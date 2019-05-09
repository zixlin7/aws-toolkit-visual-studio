using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;

namespace BuildCommon
{
    public class TestCredentials
    {
        private static List<TestCredentials> _cache = null;
        private static XmlSerializer _cacheSerializer = new XmlSerializer(typeof(List<TestCredentials>));

        public static TestCredentials GetCredentials(string id)
        {
            if (_cache == null)
            {
                Load();
            }

            TestCredentials credentials = _cache.FirstOrDefault(
                c => string.Equals(c.Id, id, StringComparison.Ordinal));
            if (credentials == null)
            {
                throw new InvalidOperationException("Invalid id");
            }
            return credentials;
        }

        private static string DefaultCredentialsId = "default";
        public static TestCredentials DefaultCredentials
        {
            get
            {
                return GetCredentials(DefaultCredentialsId);
            }
        }

        private static string CredentialsLocation = @"c:\AWS\aws-credentials.xml";

        private static void Load()
        {
            if (File.Exists(CredentialsLocation))
            {
                //Console.WriteLine("Credentials file found at at [{0}], trying to load.", CredentialsLocation);
                Load(CredentialsLocation);
            }
            else
            {
                throw new InvalidOperationException(string.Format("Cannot locate credentials at [{0}]", CredentialsLocation));
            }
        }
        private static void Load(string xmlPath)
        {
            using (Stream fileStream = File.OpenRead(xmlPath))
            {
                _cache = _cacheSerializer.Deserialize(fileStream) as List<TestCredentials>;
            }
        }
        private static void Save()
        {
            Save(CredentialsLocation);
        }
        private static void Save(string xmlPath)
        {
            using (Stream fileStream = File.OpenWrite(xmlPath))
            {
                _cacheSerializer.Serialize(fileStream, _cache);
            }
        }

        // FOR TESTING ONLY
        private static void Save(TestCredentials credentials)
        {
            TestCredentials oldCreds = null;
            try
            {
                oldCreds = GetCredentials(credentials.Id);
            }
            catch { }
            if (oldCreds != null)
            {
                _cache.Remove(oldCreds);
            }

            _cache.Add(credentials);
            Save(CredentialsLocation);
        }

        #region Properties

        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string SessionToken { get; set; }
        public string AccountId { get; set; }
        public string Id { get; set; }

        #endregion
    }
}
