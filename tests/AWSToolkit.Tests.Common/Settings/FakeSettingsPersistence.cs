using System.Collections.Generic;
using Amazon.AWSToolkit.Settings;

namespace Amazon.AWSToolkit.Tests.Common.Settings
{
    /// <summary>
    /// In-memory implementation of SettingsPersistenceBase, used to
    /// fake out SettingsPersistenceBase in tests.
    ///
    /// To use, call ToolkitSettings.Initialize with an instance of
    /// FakeSettingsPersistence in your tests.
    /// </summary>
    public class FakeSettingsPersistence : SettingsPersistenceBase
    {
        public readonly Dictionary<string, string> PersistenceData = new Dictionary<string, string>();

        public override string GetString(string key)
        {
            if (PersistenceData.TryGetValue(key, out string value))
            {
                return value;
            }

            return null;
        }

        public override void SetString(string key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                PersistenceData.Remove(key);
            }
            else
            {
                PersistenceData[key] = value;
            }
        }
    }
}