using System;
using System.IO;
using Amazon.AWSToolkit.Account.Model;
using Amazon.Runtime.Internal.Settings;
using log4net;
using ThirdParty.Json.LitJson;

namespace Amazon.AWSToolkit.Util
{
    public class ServiceSpecificCredentialStore
    {
        private static readonly ILog LOGGER = LogManager.GetLogger(typeof(ServiceSpecificCredentialStore));
        private static readonly ServiceSpecificCredentialStore _instance = new ServiceSpecificCredentialStore();

        public static readonly string CodeCommitServiceName = "codecommit";

        private ServiceSpecificCredentialStore()
        {
        }

        public static ServiceSpecificCredentialStore Instance => _instance;

        public ServiceSpecificCredentials GetCredentialsForService(string accountArtifactsId, string serviceName)
        {
            var fullpath = ConstructArtifactsFilePath(accountArtifactsId, serviceName, false);
            if (!File.Exists(fullpath))
                return null;

            string encryptedCredentials;
            using (var reader = new StreamReader(fullpath))
            {
                encryptedCredentials = reader.ReadToEnd();
            }

            try
            {
                return ServiceSpecificCredentials.FromJson(encryptedCredentials);
            }
            catch (Exception e)
            {
                LOGGER.ErrorFormat("Failed to load {0} service credentials for account {1}, exception {2}", serviceName, accountArtifactsId, e);
                throw;
            }
        }

        public ServiceSpecificCredentials SaveCredentialsForService(string accountArtifactsId, string serviceName, string userName, string password)
        {
            var serviceCredentials = new ServiceSpecificCredentials
            {
                Username = userName,
                Password = password
            };

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
            catch (Exception e)
            {
                LOGGER.Error("Failed to write credentials data to settings folder", e);
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
            var fullpath = string.Format(@"{0}\{1}.encrypted", accountLocation, serviceName);
            return fullpath;
        }

        private static string GetDirectory(string accountArtifactsId, bool autoCreateFolder)
        {
            var settingsFolder = PersistenceManager.GetSettingsStoreFolder();
            var location = string.Format(@"{0}\servicecredentials\{1}", settingsFolder, accountArtifactsId);

            if (!Directory.Exists(location) && autoCreateFolder)
                Directory.CreateDirectory(location);

            return location;
        }
    }


    public class ServiceSpecificCredentials
    {
        public string Username { get; internal set; }
        public string Password { get; internal set; }

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

            jsonWriter.WriteObjectEnd();

            return jsonWriter.ToString();
        }

        internal static ServiceSpecificCredentials FromJson(string encryptedJson)
        {
            var jdata = JsonMapper.ToObject(encryptedJson);
            return new ServiceSpecificCredentials
            {
                Username = UserCrypto.Decrypt((string)jdata[ToolkitSettingsConstants.ServiceCredentialsUserName]),
                Password = UserCrypto.Decrypt((string)jdata[ToolkitSettingsConstants.ServiceCredentialsPassword])
            };
        }

        // Called to process a csv file created by us as we drive the user credential process, therefore
        // we don't expect the csv file to fail to load
        public static ServiceSpecificCredentials FromCsvFile(string csvFile)
        {
            string username, password;
            RegisterServiceCredentialsModel.ImportCredentialsFromCsv(csvFile, out username, out password);
            return new ServiceSpecificCredentials
            {
                Username = username,
                Password = password
            };
        }

        public static ServiceSpecificCredentials FromCredentials(string username, string password)
        {
            return new ServiceSpecificCredentials
            {
                Username = username,
                Password = password
            };
        }
    }

}
