using System;
using System.IO;
using Amazon.AWSToolkit.Account;
using Amazon.Runtime.Internal.Settings;

using log4net;

namespace Amazon.AWSToolkit.Util
{
    public class KeyPairLocalStoreManager
    {
        ILog LOGGER = LogManager.GetLogger(typeof(KeyPairLocalStoreManager));
        static KeyPairLocalStoreManager INSTANCE = new KeyPairLocalStoreManager();
        private KeyPairLocalStoreManager()
        {
        }

        public static KeyPairLocalStoreManager Instance => INSTANCE;

        public string GetPrivateKey(AccountViewModel account, string region, string keyPairName)
        {
            try
            {
                string fullpath = getFullPath(account.SettingsUniqueKey, region, keyPairName);
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
                LOGGER.Error("Failed to read private key from settings folder", e);
                return null;
            }
        }
            

        public void SavePrivateKey(AccountViewModel account, string region, string keyPairName, string privateKey)
        {
            try
            {
                string encryptedPrivateKey = UserCrypto.Encrypt(privateKey);
                string fullpath = getFullPath(account.SettingsUniqueKey, region, keyPairName);
                using(StreamWriter writer = new StreamWriter(fullpath))
                {
                    writer.Write(encryptedPrivateKey);
                }
            }
            catch(Exception e)
            {
                LOGGER.Error("Failed to write private key to settings folder", e);
            }
        }

        public void ClearPrivateKey(AccountViewModel account, string region, string keyPairName)
        {
            string fullpath = getFullPath(account.SettingsUniqueKey, region, keyPairName);
            if (File.Exists(fullpath))
            {
                File.Delete(fullpath);
                LOGGER.InfoFormat("Deleting key pair {0} in region {1} for profile", keyPairName, region, account.AccountDisplayName);
            }
        }

        public bool DoesPrivateKeyExist(AccountViewModel account, string region, string keyPairName)
        {
            string fullpath = getFullPath(account.SettingsUniqueKey, region, keyPairName);
            return File.Exists(fullpath);
        }

        string getFullPath(string accountId, string region, string keyPairName)
        {
            string keyLocation = getDirectory(accountId, region, keyPairName);
            string fullpath = string.Format(@"{0}\{1}.pem.encrypted", keyLocation, keyPairName);
            return fullpath;
        }

        string getDirectory(string accountId, string region, string keyPairName)
        {
            string settingsFolder = PersistenceManager.GetSettingsStoreFolder();
            string keyLocation = string.Format(@"{0}\keypairs\{1}\{2}", settingsFolder, accountId, region);
            if (!Directory.Exists(keyLocation))
                Directory.CreateDirectory(keyLocation);

            return keyLocation;
        }
    }
}
