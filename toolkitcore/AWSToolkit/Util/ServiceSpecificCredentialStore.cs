using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Xml.Linq;

using Amazon.AWSToolkit.Account.Model;
using Amazon.Runtime.Internal.Settings;
using log4net;
using ThirdParty.Json.LitJson;

namespace Amazon.AWSToolkit.Util
{
    public class ServiceSpecificCredentialStore
    {
        private static readonly ILog LOGGER = LogManager.GetLogger(typeof(ServiceSpecificCredentialStore));

        static ServiceSpecificCredentialStore()
        {
            Instance = new ServiceSpecificCredentialStore();
        }

        private ServiceSpecificCredentialStore() { }

        public static ServiceSpecificCredentialStore Instance { get; }

        public ServiceSpecificCredentials GetCredentialsForService(string accountArtifactsId, string serviceName)
        {
            var fullpath = ConstructArtifactsFilePath(accountArtifactsId, serviceName, false);
            if (!File.Exists(fullpath))
            {
                return null;
            }

            string encryptedCredentials;
            using (var reader = new StreamReader(fullpath))
            {
                encryptedCredentials = reader.ReadToEnd();
            }

            try
            {
                return ServiceSpecificCredentials.FromJson(encryptedCredentials);
            }
            catch (Exception ex)
            {
                LOGGER.ErrorFormat("Failed to load {0} service credentials for account {1}, exception {2}", serviceName, accountArtifactsId, ex);
                throw;
            }
        }

        public bool TryGetCredentialsForService(string accountArtifactsId, string serviceName, out ServiceSpecificCredentials creds)
        {
            try
            {
                creds = GetCredentialsForService(accountArtifactsId, serviceName);
            }
            catch
            {
                creds = null;
            }

            return creds != null;
        }

        public ServiceSpecificCredentials SaveCredentialsForService(string accountArtifactsId, string serviceName, string userName, string password, DateTime? expiresOn = null)
        {
            var serviceCredentials = new ServiceSpecificCredentials(userName, password, expiresOn);
            return SaveCredentialsForService(accountArtifactsId, serviceName, serviceCredentials);
        }

        public ServiceSpecificCredentials SaveCredentialsForService(string accountArtifactsId, string serviceName, ServiceSpecificCredentials serviceCredentials)
        {
            try
            {
                var encryptedCredentials = serviceCredentials.ToJson();
                var fullpath = ConstructArtifactsFilePath(accountArtifactsId, serviceName, true);
                using (var writer = new StreamWriter(fullpath))
                {
                    writer.Write(encryptedCredentials);
                }
            }
            catch (Exception ex)
            {
                LOGGER.Error("Failed to write credentials data to settings folder", ex);
            }

            return serviceCredentials;
        }

        public void ClearCredentialsForService(string accountArtifactsId, string serviceName)
        {
            var fullpath = ConstructArtifactsFilePath(accountArtifactsId, serviceName, false);
            if (File.Exists(fullpath))
            {
                File.Delete(fullpath);
                LOGGER.InfoFormat("Deleting credentials for {0} for account setttings {1}", serviceName, accountArtifactsId);
            }
        }

        public bool ServiceSpecificCredentialsExist(string accountArtifactsId, string serviceName)
        {
            var fullpath = ConstructArtifactsFilePath(accountArtifactsId, serviceName, false);
            return File.Exists(fullpath);
        }

        private static string ConstructArtifactsFilePath(string accountArtifactsId, string serviceName, bool autoCreate)
        {
            var accountLocation = GetDirectory(accountArtifactsId, autoCreate);
            return string.Format(@"{0}\{1}.encrypted", accountLocation, serviceName);
        }

        private static string GetDirectory(string accountArtifactsId, bool autoCreateFolder)
        {
            var settingsFolder = PersistenceManager.GetSettingsStoreFolder();
            var location = string.Format(@"{0}\servicecredentials\{1}", settingsFolder, accountArtifactsId);

            if (!Directory.Exists(location) && autoCreateFolder)
            {
                Directory.CreateDirectory(location);
            }

            return location;
        }
    }

    public class ServiceSpecificCredentials
    {
        internal ServiceSpecificCredentials(string username, string password, DateTime? expiresOn = null)
        {
            Arg.NotNull(username, nameof(username));
            Arg.NotNull(password, nameof(password));

            Username = username;
            Password = password;
            ExpiresOn = expiresOn;
        }

        public string Username { get; }

        public string Password { get; }

        public DateTime? ExpiresOn { get; }

        internal string ToJson()
        {
            var jsonWriter = new JsonWriter
            {
                PrettyPrint = true
            };

            jsonWriter.WriteObjectStart();

            jsonWriter.WritePropertyName(ToolkitSettingsConstants.ServiceCredentialsUserName);
            jsonWriter.Write(UserCrypto.Encrypt(Username));

            jsonWriter.WritePropertyName(ToolkitSettingsConstants.ServiceCredentialsPassword);
            jsonWriter.Write(UserCrypto.Encrypt(Password));

            if (ExpiresOn != null)
            {
                var binary = ExpiresOn.Value.ToBinary();
                var bytes = BitConverter.GetBytes(binary);
                var base64 = Convert.ToBase64String(bytes);
                var encrypted = UserCrypto.Encrypt(base64);

                jsonWriter.WritePropertyName(ToolkitSettingsConstants.ServiceCredentialsExpiresOn);
                jsonWriter.Write(encrypted);
            }

            jsonWriter.WriteObjectEnd();

            return jsonWriter.ToString();
        }

        internal static ServiceSpecificCredentials FromJson(string encryptedJson)
        {
            var jdata = JsonMapper.ToObject(encryptedJson);

            var username = UserCrypto.Decrypt((string) jdata[ToolkitSettingsConstants.ServiceCredentialsUserName]);
            var password = UserCrypto.Decrypt((string) jdata[ToolkitSettingsConstants.ServiceCredentialsPassword]);
            DateTime? expiresOn = null;

            if (jdata.PropertyNames.Contains(ToolkitSettingsConstants.ServiceCredentialsExpiresOn))
            {
                var encrypted = (string) jdata[ToolkitSettingsConstants.ServiceCredentialsExpiresOn];
                var base64 = UserCrypto.Decrypt(encrypted);
                var bytes = Convert.FromBase64String(base64);
                var binary = BitConverter.ToInt64(bytes, 0);

                expiresOn = DateTime.FromBinary(binary);
            }

            return new ServiceSpecificCredentials(username, password, expiresOn);
        }

        // Called to process a csv file created by us as we drive the user credential process, therefore
        // we don't expect the csv file to fail to load
        public static ServiceSpecificCredentials FromCsvFile(string csvFile)
        {
            RegisterServiceCredentialsModel.ImportCredentialsFromCsv(csvFile, out string username, out string password);
            return new ServiceSpecificCredentials(username, password);
        }

        public static ServiceSpecificCredentials FromCredentials(string username, string password)
        {
            return new ServiceSpecificCredentials(username, password);
        }

        protected bool Equals(ServiceSpecificCredentials other)
        {
            return
                Username == other.Username
                && Password == other.Password
                && Nullable.Equals(ExpiresOn, other.ExpiresOn);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((ServiceSpecificCredentials) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Username != null ? Username.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Password != null ? Password.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ExpiresOn.GetHashCode();

                return hashCode;
            }
        }
    }
}
