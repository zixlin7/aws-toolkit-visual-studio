using System;

using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Util;

using log4net;

namespace Amazon.AWSToolkit.VisualStudio.SupportedVersion
{
    /// <summary>
    /// Manages the concept of whether or not information about minimum supported version
    /// has been or should be shown to the user
    /// </summary>
    public class SupportedVersionStrategy
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(SupportedVersionStrategy));

        private readonly ToolkitSettings _toolkitSettings;

        // Notice version affords the opportunity to re-show the notice in the future when reasons (e.g. message text) arise.
        public const int CurrentVs2017NoticeVersion = 1;

        /// <summary>
        /// Represents the current host
        /// </summary>
        public IToolkitHostInfo CurrentHost { get; set; }

        /// <summary>
        /// Represents the shell/host being deprecated
        /// </summary>
        public IToolkitHostInfo HostDeprecated { get; set; }

        public SupportedVersionStrategy(IToolkitHostInfo currentHost, IToolkitHostInfo hostDeprecated, ToolkitSettings toolkitSettings)
        {
            CurrentHost = currentHost;
            HostDeprecated = hostDeprecated;
            _toolkitSettings = toolkitSettings;
        }

        public bool CanShowNotice()
        {
            if (IsSupportedVersion())
            {
                return false;
            }

            return !HasUserSeenNotice();
        }

        public bool IsSupportedVersion()
        {
            if (CurrentHost == HostDeprecated)
            {
                return false;
            }

            return true;
        }

        public bool HasUserSeenNotice()
        {
            if (HostDeprecated == ToolkitHosts.Vs2017)
            {
                return _toolkitSettings.Vs2017SunsetNoticeVersionShown >= CurrentVs2017NoticeVersion;
            }

            return true;
        }

        public void MarkNoticeAsShown()
        {
            try
            {
                // Skip if user has seen a newer notice version
                if (!HasUserSeenNotice())
                {
                    MarkSettingAsShown();
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Error showing deprecation notice for {HostDeprecated.Version}", e);
            }
        }

        public string GetMessage()
        {
            if (HostDeprecated == ToolkitHosts.Vs2017)
            {
                return
                    "AWS Toolkit is deprecating support for Visual Studio 2017. Upcoming releases will require Visual Studio 2019.";
            }

            return string.Empty;
        }

        private void MarkSettingAsShown()
        {
            if (HostDeprecated == ToolkitHosts.Vs2017)
            {
                _toolkitSettings.Vs2017SunsetNoticeVersionShown = CurrentVs2017NoticeVersion;
            }
        }
    }
}
