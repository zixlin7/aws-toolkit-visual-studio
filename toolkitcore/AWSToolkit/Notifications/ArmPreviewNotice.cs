using System;
using System.Runtime.InteropServices;

using Amazon.AWSToolkit.Settings;
using log4net;

namespace Amazon.AWSToolkit.Notifications
{
    /// <summary>
    /// Manages the concept of whether or not information has been or should be
    /// shown to the user.
    /// </summary>
    public static class ArmPreviewNotice
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ArmPreviewNotice));

        // Notice version affords the opportunity to re-show the notice in the future when reasons arise.
        private const int _currentNoticeVersion = 1;

        public static bool CanShowNotice()
        {
            var osArch = RuntimeInformation.OSArchitecture;
            if (osArch != Architecture.Arm && osArch != Architecture.Arm64)
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
            return ToolkitSettings.Instance.Arm64PreviewNoticeVersionShown >= _currentNoticeVersion;
        }

        public static void MarkNoticeAsShown()
        {
            try
            {
                // Skip if user has seen a newer notice version
                if (!HasUserSeenNotice())
                {
                    ToolkitSettings.Instance.Arm64PreviewNoticeVersionShown = _currentNoticeVersion;
                }
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }
    }
}
