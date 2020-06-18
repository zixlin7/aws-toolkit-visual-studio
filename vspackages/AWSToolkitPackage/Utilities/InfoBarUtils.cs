using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Amazon.AWSToolkit.VisualStudio.Utilities
{
    public static class InfoBarUtils
    {
        /// <summary>
        /// Returns the InfoBar host for the IDE's main window, if available.
        /// </summary>
        /// <remarks>
        /// If the main window is not available (eg: not shown yet), null is returned.
        /// </remarks>
        /// <returns>IVsInfoBarHost, null if not available</returns>
        public static IVsInfoBarHost GetMainWindowInfoBarHost(IServiceProvider serviceProvider)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var vsShell = serviceProvider.GetService(typeof(SVsShell)) as IVsShell;
            if (vsShell == null)
            {
                return null;
            }

            var result = vsShell.GetProperty((int) __VSSPROPID7.VSSPROPID_MainWindowInfoBarHost,
                out var mainWindowHostObj);
            if (result != VSConstants.S_OK)
            {
                return null;
            }

            return (IVsInfoBarHost) mainWindowHostObj;
        }

        /// <summary>
        /// Creates an InfoBar element that contains the provided InfoBar model
        /// </summary>
        /// <returns>InfoBar element, null if unable to create</returns>
        public static IVsInfoBarUIElement CreateInfoBar(IVsInfoBar infoBarModel, IServiceProvider serviceProvider)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var vsInfoBarUiFactory = serviceProvider.GetService(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;

            return vsInfoBarUiFactory?.CreateInfoBar(infoBarModel);
        }
    }
}