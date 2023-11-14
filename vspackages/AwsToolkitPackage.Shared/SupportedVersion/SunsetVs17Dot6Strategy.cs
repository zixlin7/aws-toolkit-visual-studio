using System;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Urls;

namespace Amazon.AWSToolkit.VisualStudio.SupportedVersion
{
    /// <summary>
    /// Sunset strategy for notifying users running 17.0 - 17.6.
    /// We're moving to 17.7 to leverage VS SDK suggestion APIs.
    /// </summary>
    public class SunsetVs17Dot6Strategy : SunsetNotificationStrategy
    {
        public override string Identifier => "VS2022_MinVer_17.7";

        protected override Version _futureMinimumRequiredVersion { get; } = new Version(17, 7);

        public SunsetVs17Dot6Strategy(Version vsVersion) : base(vsVersion)
        {
        }

        /// <summary>
        /// Constructor Overload for testing purposes
        /// </summary>
        public SunsetVs17Dot6Strategy(Version vsVersion, ISettingsRepository<SunsetNotificationSettings> settingsRepository) : base(vsVersion, settingsRepository)
        {
        }

        public override string GetMessage()
        {
            return "The next version of AWS Toolkit will require version 17.7 or newer of Visual Studio 2022. Please update Visual Studio in order to continue receiving AWS Toolkit updates.";
        }

        public override string GetLearnMoreUrl()
        {
            return GitHubUrls.Sunset.SunsetVs17Dot6Announcement;
        }

        public override Task<bool> CanShowNoticeAsync()
        {
#if VS2022
            // check if 17.7 or newer is running
            return base.CanShowNoticeAsync();
#else
            // This strategy is not applicable to VS 2019 (and other major versions)
            return Task.FromResult(false);
#endif
        }
    }
}
