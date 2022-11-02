using Amazon.AWSToolkit.Settings;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Settings
{
    public class LoggingSettingsTests 
    {
        [Fact]
        public void CreatesDefault()
        {
            var expectedDefaultSettings = new LoggingSettings()
            {
                LogFileRetentionMonths = LoggingSettings.DefaultValues.LogFileRetentionMonths,
                MaxLogDirectorySizeMb = LoggingSettings.DefaultValues.MaxLogDirectorySizeMb,
                MaxLogFileSizeMb = LoggingSettings.DefaultValues.MaxLogFileSizeMb,
                MaxFileBackups = LoggingSettings.DefaultValues.MaxFileBackups
            };
            Assert.Equal(expectedDefaultSettings, new LoggingSettings());
        }
    }
}
