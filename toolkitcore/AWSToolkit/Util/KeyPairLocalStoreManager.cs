using System;
using System.IO;
using Amazon.AWSToolkit.Account;
using Amazon.Runtime.Internal.Settings;

using log4net;

namespace Amazon.AWSToolkit.Util
{
    public class KeyPairLocalStoreManager
    {
        private ILog Logger = LogManager.GetLogger(typeof(KeyPairLocalStoreManager));

        public static readonly KeyPairLocalStoreManager Instance = new KeyPairLocalStoreManager();

        private KeyPairLocalStoreManager() { }

        public string GetPrivateKey(string settingsUniqueKey, string region, string keyPairName)
        {
            try
            {
                string fullpath = getFullPath(settingsUniqueKey, region, keyPairName);
                if (!File.Exists(fullpath))
                    return null;

                string encryptedPrivateKey = null;
                using (StreamReader reader = new StreamReader(fullpath))
                {
                    encryptedPrivateKey = reader.ReadToEnd();
                }

                return UserCrypto.Decrypt(encryptedPrivateKey);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to read private key from settings folder", e);
                return null;
            }
        }

        public void SavePrivateKey(string settingsUniqueKey, string region, string keyPairName, string privateKey)
        {
            try
            {
                string encryptedPrivateKey = UserCrypto.Encrypt(privateKey);
                string fullpath = getFullPath(settingsUniqueKey, region, keyPairName);
                using(StreamWriter writer = new StreamWriter(fullpath))
                {
                    writer.Write(encryptedPrivateKey);
                }
            }
            catch(Exception e)
            {
                Logger.Error("Failed to write private key to settings folder", e);
            }
        }

        public void ClearPrivateKey(string settingsUniqueKey, string region, string keyPairName)
        {
            string fullpath = getFullPath(settingsUniqueKey, region, keyPairName);
            if (File.Exists(fullpath))
            {
                File.Delete(fullpath);
                Logger.InfoFormat("Deleting key pair {0} in region {1} for profile", keyPairName, region);
            }
        }

        public bool DoesPrivateKeyExist(string settingsUniqueKey, string region, string keyPairName)
        {
            string fullpath = getFullPath(settingsUniqueKey, region, keyPairName);
            return File.Exists(fullpath);
        }

        private string getFullPath(string accountId, string region, string keyPairName)
        {
            string keyLocation = getDirectory(accountId, region, keyPairName);
            string fullpath = string.Format(@"{0}\{1}.pem.encrypted", keyLocation, keyPairName);
            return fullpath;
        }

        private string getDirectory(string accountId, string region, string keyPairName)
        {
            string settingsFolder = PersistenceManager.GetSettingsStoreFolder();
            string keyLocation = string.Format(@"{0}\keypairs\{1}\{2}", settingsFolder, accountId, region);
            if (!Directory.Exists(keyLocation))
            {
                Directory.CreateDirectory(keyLocation);
            }

            return keyLocation;
        }
    }
}
