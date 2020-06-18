using Amazon.Runtime.Internal.Settings;

namespace Amazon.AWSToolkit.Settings
{
    /**
     * Use this class in systems that need to mock PersistenceManager
     */
    public class SettingsPersistence
    {
        private readonly PersistenceManager _persistenceManager;

        public SettingsPersistence()
        {
            _persistenceManager = PersistenceManager.Instance;
        }

        public virtual string GetSetting(string key)
        {
            return _persistenceManager.GetSetting(key);
        }

        public virtual void SetSetting(string key, string value)
        {
            _persistenceManager.SetSetting(key, value);
        }
    }
}