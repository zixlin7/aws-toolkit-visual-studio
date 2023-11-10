using System.Threading.Tasks;

using Amazon.AWSToolkit.Settings;

namespace Amazon.AWSToolkit.Tests.Common.Settings
{
    public class FakeSettingsRepository<T> : ISettingsRepository<T> where T : class, new()
    {
        public T Settings { get; set; }

        public Task<T> GetOrDefaultAsync(T defaultValue = default)
        {
            return Task.FromResult(Settings);
        }

        public void Save(T settings)
        {
            Settings = settings;
        }
    }
}
