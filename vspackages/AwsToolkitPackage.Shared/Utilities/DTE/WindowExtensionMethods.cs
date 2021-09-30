using System;
using System.Windows.Forms;
using log4net;
using Microsoft.VisualStudio.Shell;

namespace Amazon.AWSToolkit.VisualStudio.Utilities.DTE
{
    public static class WindowExtensionMethods
    {
        private static readonly ILog LOGGER = LogManager.GetLogger(typeof(WindowExtensionMethods));

        /// <summary>
        /// Determines which screen the main VS window is in, defaulting to Primary if necessary.
        /// </summary>
        public static Screen GetScreen(this EnvDTE.Window window)
        {
            var screen = Screen.PrimaryScreen;

            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (window == null)
                {
                    return screen;
                }

                screen = Screen.FromPoint(new System.Drawing.Point(window.Left, window.Top));
            }
            catch (Exception e)
            {
                LOGGER.Error(e);
            }

            return screen;
        }
    }
}