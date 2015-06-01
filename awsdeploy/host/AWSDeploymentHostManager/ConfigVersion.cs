using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AWSDeploymentHostManager.Persistence;
using ThirdParty.Json.LitJson;

namespace AWSDeploymentHostManager
{
    public class ConfigVersion : Version
    {
        public const string JSON_ISAPPLICATION_INSTALLED = "isAppInstalled";
        public const string JSON_APPLICATION_INSTALLED_TIMESTAMP = "appInstalledTimestamp";

        public ConfigVersion(string url, string key, string iv, bool isApplicationInstalled)
        {
            vers = new EntityObject(EntityType.ConfigVersion);
            Url = url;
            CryptoKey = key;
            CryptoIV = iv;
            IsApplicationVersionInstalled = isApplicationInstalled;
            ParseUrl();
        }

        public ConfigVersion(string bucket, string key)
        {
            vers = new EntityObject(EntityType.ConfigVersion);
            S3Bucket = bucket;
            S3Key = key;
        }

        public ConfigVersion(EntityObject eo)
        {
            if (eo.EntityType != EntityType.ConfigVersion)
                throw new ArgumentException("Attempted to create an ConfigVersion with an EntiyObject of a different type.");

            vers = eo;
        }

        public bool IsApplicationVersionInstalled
        {
            get
            {
                string s = null;
                if (!vers.Parameters.TryGetValue(JSON_ISAPPLICATION_INSTALLED, out s))
                    return true; // For backwards compatible will assume if the value is not here it is already installed.
                return bool.Parse(s);
            }
            private set
            {
                vers.Parameters[JSON_ISAPPLICATION_INSTALLED] = value.ToString();
            }
        }

        public DateTime ApplicationInstallTimestamp
        {
            get
            {
                string s = null;
                if (!vers.Parameters.TryGetValue(JSON_APPLICATION_INSTALLED_TIMESTAMP, out s))
                    return DateTime.MinValue;
                return DateTime.Parse(s);
            }
            private set
            {
                vers.Parameters[JSON_APPLICATION_INSTALLED_TIMESTAMP] = value.ToString();
            }
        }

        public void MarkApplicationVersionInstalled()
        {
            this.IsApplicationVersionInstalled = true;
            ApplicationInstallTimestamp = DateTime.Now;
        }

        public void Persist()
        {
            PersistenceManager pm = new PersistenceManager();
            pm.Persist(vers);
        }

        public static ConfigVersion LoadLatestVersion()
        {
            PersistenceManager pm = new PersistenceManager();
            EntityObject eo = pm.LoadLatest(EntityType.ConfigVersion);
            if (eo != null)
                return new ConfigVersion(eo);

            return null;
        }
    }
}
