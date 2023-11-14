using System;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Settings;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Amazon.AWSToolkit.VisualStudio.SupportedVersion
{
    /// <summary>
    /// Represents the knowledge that the minimum supported version of Visual Studio
    /// will be increased in an upcoming release.
    /// This is intended to be used for sun-setting versions within a Major Version band.
    /// For example, raising the minimum version from 17.0 to 17.7.
    /// </summary>
    public interface ISunsetNotificationStrategy
    {
        /// <summary>
        /// Uniquely identifies the strategy (eg: logging, settings, ...)
        /// </summary>
        string Identifier { get; }

        /// <summary>
        /// User facing text to display in the notification (InfoBar)
        /// </summary>
        /// <returns></returns>
        string GetMessage();

        /// <summary>
        /// Optional link to URL where users can learn more about the sunset
        /// </summary>
        string GetLearnMoreUrl();

        /// <summary>
        /// User facing icon to display in the notification (InfoBar)
        /// </summary>
        ImageMoniker Icon { get; }

        /// <summary>
        /// Used to persist that this notification should not be displayed again
        /// </summary>
        Task MarkAsSeenAsync();

        /// <summary>
        /// Whether or not it is valid to show this notice to the user
        /// </summary>
        Task<bool> CanShowNoticeAsync();
    }

    /// <summary>
    /// Base implementation for sunset notification.
    ///
    /// Provides consistent handling for things like settings. Implementors only need to
    /// derive the class and provide their message and new-minimum-version of interest.
    /// </summary>
    public abstract class SunsetNotificationStrategy : ISunsetNotificationStrategy
    {
        private static readonly string _defaultSettingsPath = ToolkitAppDataPath.Join("SunsetNotificationSettings.json");

        public abstract string Identifier { get; }

        public ImageMoniker Icon { get; } = KnownMonikers.StatusInformation;

        // We "version" the notification. This allows us to re-display the notice in the future, if we discover a
        // valid reason to do so.
        // Each implementation is versioned independently, but starts with a base version of 1.
        protected int _noticeVersion = 1;

        /// <summary>
        /// Indicates what the new minimum supported version of Visual Studio will be
        /// </summary>
        protected abstract Version _futureMinimumRequiredVersion { get; }

        private readonly ISettingsRepository<SunsetNotificationSettings> _settingsRepository;
        private readonly Version _vsVersion;

        protected SunsetNotificationStrategy(Version vsVersion)
            : this(vsVersion, new FileSettingsRepository<SunsetNotificationSettings>(_defaultSettingsPath))
        {
        }

        /// <summary>
        /// Constructor overload, used for testing
        /// </summary>
        protected SunsetNotificationStrategy(
            Version vsVersion,
            ISettingsRepository<SunsetNotificationSettings> settingsRepository)
        {
            _settingsRepository = settingsRepository;
            _vsVersion = vsVersion;
        }

        public abstract string GetMessage();

        public virtual string GetLearnMoreUrl()
        {
            return string.Empty;
        }

        public virtual async Task MarkAsSeenAsync()
        {
            var settings = await _settingsRepository.GetOrDefaultAsync();

            if (settings == null)
            {
                settings = new SunsetNotificationSettings();
            }

            settings.SetDisplayedNotificationVersion(Identifier, _noticeVersion);

            _settingsRepository.Save(settings);
        }

        public virtual async Task<bool> CanShowNoticeAsync()
        {
            if (!IsVsSunsetCandidate())
            {
                return false;
            }

            var settings = await _settingsRepository.GetOrDefaultAsync();
            if (settings == null)
            {
                return true;
            }

            // Default to "one less than the current version", so that we are guaranteed
            // to display the notice if the user has never seen it (or never dismissed it).
            return settings.GetDisplayedNotificationVersion(Identifier, _noticeVersion - 1) < _noticeVersion;
        }

        /// <summary>
        /// Whether or not the currently running instance of Visual Studio is going to
        /// be below the future minimum supported version of Visual Studio.
        /// </summary>
        protected bool IsVsSunsetCandidate()
        {
            return _vsVersion == null || _vsVersion < _futureMinimumRequiredVersion;
        }
    }
}
