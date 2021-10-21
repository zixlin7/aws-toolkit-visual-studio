using System.Threading.Tasks;

namespace Amazon.AWSToolkit.Publish.PublishSetting
{
    /// <summary>
    /// Represents a repository/file containing all publish experience related settings
    /// </summary>
    public interface IPublishSettingsRepository
    {
        Task<PublishSettings> GetAsync();

        void Save(PublishSettings publishSettings);
    }
}
