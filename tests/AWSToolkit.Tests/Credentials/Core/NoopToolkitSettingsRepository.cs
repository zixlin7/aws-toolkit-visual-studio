using Amazon.AWSToolkit.Settings;

namespace AWSToolkit.Tests.Credentials.Core
{
    public class NoopToolkitSettingsRepository : IToolkitSettingsRepository
    {
        public string GetLastSelectedCredentialId()
        {
            return null;
        }

        public string GetLastSelectedRegion()
        {
            return null;
        }
    }
}
