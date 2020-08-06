using System.Globalization;

namespace Amazon.AWSToolkit.Settings
{
    /**
     * Use this class in systems that need to mock PersistenceManager
     */
    public abstract class SettingsPersistenceBase
    {
        public abstract string GetString(string key);

        public int GetInt(string key, int defaultValue = 0)
        {
            var valueStr = GetString(key);
            if (!int.TryParse(valueStr, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            {
                return defaultValue;
            }

            return value;
        }

        public abstract void SetString(string key, string value);

        public void SetInt(string key, int value)
        {
            var valueStr = value.ToString(CultureInfo.InvariantCulture);
            SetString(key, valueStr);
        }
    }
}