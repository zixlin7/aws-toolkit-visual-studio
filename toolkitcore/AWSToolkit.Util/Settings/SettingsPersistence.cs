using System.Globalization;
using Amazon.Runtime.Internal.Settings;
using log4net;

namespace Amazon.AWSToolkit.Settings
{
    /**
     * Use this class in systems that need to mock PersistenceManager
     */
    public class SettingsPersistence : SettingsPersistenceBase
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(SettingsPersistence));
        private readonly PersistenceManager _persistenceManager;

        public SettingsPersistence()
        {
            _persistenceManager = PersistenceManager.Instance as PersistenceManager;
            if (_persistenceManager == null)
            {
                Logger.Error("Unable to access Toolkit Settings");
            }
        }

        public override string GetString(string key)
        {
            return _persistenceManager.GetSetting(key);
        }

        public override void SetString(string key, string value)
        {
            _persistenceManager.SetSetting(key, value);
        }
    }
}