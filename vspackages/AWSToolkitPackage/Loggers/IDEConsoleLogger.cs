using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.VisualStudio.Services;
using Amazon.AWSToolkit.Shared;

namespace Amazon.AWSToolkit.VisualStudio.Loggers
{
    /// <summary>
    /// Routes messages from build/deployment process to 'AWS' tab in the IDE
    /// </summary>
    internal class IDEConsoleLogger : IBuildAndDeploymentLogger
    {
        readonly IAWSToolkitShellProvider _toolkitShellProvider;

        internal IDEConsoleLogger(IAWSToolkitShellProvider toolkitShellProvider)
        {
            this._toolkitShellProvider = toolkitShellProvider;
        }

        #region IDeploymentLogger

        void IBuildAndDeploymentLogger.OutputMessage(string message)
        {
            (this as IBuildAndDeploymentLogger).OutputMessage(message, false, false);
        }

        void IBuildAndDeploymentLogger.OutputMessage(string message, bool addToLog)
        {
            (this as IBuildAndDeploymentLogger).OutputMessage(message, addToLog, false);
        }

        void IBuildAndDeploymentLogger.OutputMessage(string message, bool addToLog, bool requestForceVisible)
        {
            // although we can request the window containing messages be forced visible,
            // this is not guaranteed.
            _toolkitShellProvider.OutputToHostConsole(message, requestForceVisible);
            if (addToLog)
                _toolkitShellProvider.AddToLog("info", message);
        }

        void IBuildAndDeploymentLogger.LogMessage(string message)
        {
            _toolkitShellProvider.AddToLog("info", message);
        }

        #endregion

        private IDEConsoleLogger() { }
    }
}
