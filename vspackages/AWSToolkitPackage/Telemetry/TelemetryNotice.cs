using System;
using Amazon.AWSToolkit.Settings;
using log4net;

namespace Amazon.AWSToolkit.VisualStudio.Telemetry
{
    /// <summary>
    /// Manages the concept of whether or not information has been or should be
    /// shown to the user.
    /// </summary>
    public static class TelemetryNotice
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(TelemetryNotice));

        // Notice version affords the opportunity to re-show the notice in the future when reasons arise.
        private const int CurrentNoticeVersion = 1;

        public static bool CanShowNotice()
        {
            if (!ToolkitSettings.Instance.TelemetryEnabled)
            {
                return false;
            }

            if (HasUserSeenNotice())
            {
                return false;
            }

            return true;
        }

        public static bool HasUserSeenNotice()
        {
            return ToolkitSettings.Instance.TelemetryNoticeVersionShown >= CurrentNoticeVersion;
        }

        public static void MarkNoticeAsShown()
        {
            try
            {
                // Skip if user has seen a newer notice version
                if (!HasUserSeenNotice())
                {
                    ToolkitSettings.Instance.TelemetryNoticeVersionShown = CurrentNoticeVersion;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}