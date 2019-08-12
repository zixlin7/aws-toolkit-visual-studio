using System;
using System.IO;
using System.Collections.Generic;
using TemplateWizard.ThirdParty.Json.LitJson;

namespace Amazon.AWSToolkit.Persistence
{
    public class PersistenceManager
    {
        #region Private members

        static PersistenceManager INSTANCE = new PersistenceManager();
        HashSet<string> _encryptedKeys;

        #endregion


        #region Constructor

        private PersistenceManager()
        {
            this._encryptedKeys = new HashSet<string>();
            this._encryptedKeys.Add(SettingsConstants.AccessKeyField);
            this._encryptedKeys.Add(SettingsConstants.SecretKeyField);
            this._encryptedKeys.Add(SettingsConstants.SecretKeyRepository);
        }

        #endregion


        #region Public methods

        public static PersistenceManager Instance => INSTANCE;

        public SettingsCollection GetSettings(string type)
        {
            return loadSettingsType(type);
        }

        public void SaveSettings(string type, SettingsCollection settings)
        {
            saveSettingsType(type, settings);
        }

        public string GetSetting(string key)
        {
            var sc = GetSettings(SettingsConstants.MiscSettings);
            var oc = sc[SettingsConstants.MiscSettings];
            var value = oc[key];
            return value;
        }

        public void SetSetting(string key, string value)
        {
            var sc = GetSettings(SettingsConstants.MiscSettings);
            var oc = sc[SettingsConstants.MiscSettings];
            oc[key] = value;
            SaveSettings(SettingsConstants.MiscSettings, sc);
        }

        public static string GetSettingsStoreFolder()
        {
            string folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData) + "/AWSToolkit";
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return folder;
        }

        public SettingsWatcher Watch(string type)
        {
            return new SettingsWatcher(getFileFromType(type), type);
        }

        internal bool IsEncrypted(string key)
        {
            return this._encryptedKeys.Contains(key);
        }

        #endregion


        #region Private methods

        void saveSettingsType(string type, SettingsCollection settings)
        {
            string filePath = getFileFromType(type);

            if (settings == null || settings.Count == 0)
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                return;
            }

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                settings.Persist(writer);
            }
        }

        SettingsCollection loadSettingsType(string type)
        {
            string filePath = getFileFromType(type);
            if(!File.Exists(filePath))
            {
                return new SettingsCollection();
            }

            string content;
            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    content = reader.ReadToEnd();
                }
            }
            catch
            {
                return new SettingsCollection();
            }

            Dictionary<string, Dictionary<string, object>> settings = 
                JsonMapper.ToObject<Dictionary<string, Dictionary<string, object>>>(content);

            decryptAnyEncryptedValues(settings);

            return new SettingsCollection(settings);
        }

        void decryptAnyEncryptedValues(Dictionary<string, Dictionary<string, object>> settings)
        {
            foreach (var kvp in settings)
            {
                string settingsKey = kvp.Key;
                var objectCollection = kvp.Value;
                foreach (string key in new List<string>(objectCollection.Keys))
                {
                    if (IsEncrypted(key) || IsEncrypted(settingsKey))
                    {
                        string value = objectCollection[key] as string;
                        if (value != null)
                        {
                            try
                            {
                                objectCollection[key] = UserCrypto.Decrypt(value);
                            }
                            catch
                            {
                                objectCollection.Remove(key);
                            }
                        }
                    }
                }
            }
        }

        string getFileFromType(string type)
        {
            return string.Format(@"{0}\{1}.json", GetSettingsStoreFolder(), type);
        }

        #endregion
    }

    public class SettingsWatcher : IDisposable
    {
        #region Private members

        private static ICollection<FileSystemWatcher> watchers = new List<FileSystemWatcher>();
        private FileSystemWatcher watcher;
        private string type;

        #endregion


        #region Constructors

        private SettingsWatcher()
        {
            throw new NotSupportedException();
        }

        internal SettingsWatcher(string filePath, string type)
        {
            string dirPath = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileName(filePath);
            this.watcher = new FileSystemWatcher(dirPath, fileName)
            {
                EnableRaisingEvents = true
            };
            this.watcher.Changed += new FileSystemEventHandler(SettingsFileChanged);
            this.watcher.Created += new FileSystemEventHandler(SettingsFileChanged);

            this.type = type;

            watchers.Add(watcher);
        }

        #endregion


        #region Public methods

        public SettingsCollection GetSettings()
        {
            return PersistenceManager.Instance.GetSettings(this.type);
        }

        #endregion


        #region Events

        public event EventHandler SettingsChanged;

        #endregion


        #region IDisposable Members

        public void Dispose()
        {
            if (watcher != null)
            {
                watchers.Remove(watcher);
                watcher = null;
            }
        }

        #endregion


        #region Private methods

        private void SettingsFileChanged(object sender, FileSystemEventArgs e)
        {
            if (SettingsChanged != null)
                SettingsChanged(this, null);
        }
        #endregion
    }
}
