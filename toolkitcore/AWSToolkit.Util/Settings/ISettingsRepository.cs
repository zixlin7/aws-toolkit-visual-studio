using System.Threading.Tasks;

namespace Amazon.AWSToolkit.Settings
{
    /// <summary>
    /// Represents a repository/file containing toolkit related settings
    /// </summary>
    /// <typeparam name="T">Specified toolkit settings stored in the repository/file for e.g. Publish Settings, Logging settings</typeparam>
    public interface ISettingsRepository<T> where T : class
    {
        /// <summary>
        /// Retrieves the settings from the repository, if not found returns the default value
        /// </summary>
        Task<T> GetOrDefaultAsync(T defaultValue = null);

        /// <summary>
        /// Saves the settings in the repository
        /// </summary>
        void Save(T settings);
    }
}
