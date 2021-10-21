using System;
using System.Windows.Input;

using Amazon.AWSToolkit.Shared;

namespace Amazon.AWSToolkit.Commands
{
    public class LearnMoreCommandFactory
    {
        public const string UserGuideUrl = "https://docs.aws.amazon.com/toolkit-for-visual-studio/latest/user-guide/publish-experience.html";

        private LearnMoreCommandFactory() { }

        public static ICommand Create(IAWSToolkitShellProvider toolkitHost)
        {
            return new RelayCommand((obj) => OpenUserGuide(toolkitHost));
        }

        private static void OpenUserGuide(IAWSToolkitShellProvider toolkitHost)
        {
            try
            {
                TryOpenUserGuide(toolkitHost);
            }
            catch (Exception e)
            {
                throw new CommandException("Failed to open User Guide", e);
            }
        }

        private static void TryOpenUserGuide(IAWSToolkitShellProvider toolkitHost)
        {
            toolkitHost.OpenInBrowser(UserGuideUrl, preferInternalBrowser: false);
        }
    }
}
