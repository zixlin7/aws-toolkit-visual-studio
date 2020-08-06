namespace Amazon.AWSToolkit.Settings
{
    public class DynamoDbSettings
    {
        private readonly SettingsPersistenceBase _settingsPersistence;
        private const int DefaultPort = 8000;

        private static class SettingNames
        {
            public const string Port = "LastDynamoDBConfiguredPort";
        }

        public DynamoDbSettings(SettingsPersistenceBase settingsPersistence)
        {
            _settingsPersistence = settingsPersistence;
        }

        public int Port
        {
            get =>
                _settingsPersistence.GetInt(SettingNames.Port, DefaultPort);
            set
            {
                if (Port != value)
                {
                    _settingsPersistence.SetInt(SettingNames.Port, value);
                }
            }
        }
    }
}