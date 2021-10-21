using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.PublishSetting;

namespace Amazon.AWSToolkit.Tests.Common.Settings.Publish
{
    /// <summary>
    /// In memory publish settings repository for test purposes
    /// </summary>
    public class InMemoryPublishSettingsRepository : IPublishSettingsRepository
    {
        private PublishSettings _publishSettings;

        public async Task<PublishSettings> GetAsync()
        {
            return _publishSettings ?? new PublishSettings();
        }

        public void Save(PublishSettings publishSettings)
        {
            _publishSettings = publishSettings;
        }
    }
}
