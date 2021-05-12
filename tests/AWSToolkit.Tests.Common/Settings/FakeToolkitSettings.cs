using Amazon.AWSToolkit.Settings;

namespace Amazon.AWSToolkit.Tests.Common.Settings
{
    /// <summary>
    /// In-memory implementation ToolkitSettings, used to
    /// fake out ToolkitSettings in tests and avoid using
    /// the singleton instance.
    /// </summary>
    public class FakeToolkitSettings : ToolkitSettings
    {
        public static FakeToolkitSettings Create()
        {
            return new FakeToolkitSettings(new FakeSettingsPersistence());
        }

        public readonly FakeSettingsPersistence FakeSettingsPersistence;

        public FakeToolkitSettings(FakeSettingsPersistence settingsPersistence) : base(settingsPersistence)
        {
            FakeSettingsPersistence = settingsPersistence;
        }
    }
}
