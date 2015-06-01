using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.VisualStudio.Shared.ServiceInterfaces;

namespace Amazon.AWSToolkit.VisualStudio.Shared.Loggers
{
    /// <summary>
    /// Routes messages from build/deployment process to 'AWS' tab in the IDE
    /// </summary>
    internal class IDEConsoleLogger : IBuildAndDeploymentLogger
    {
        readonly IAWSToolkitService _shellToolkitService;

        internal IDEConsoleLogger(IAWSToolkitService hostShellToolkitService)
        {
            this._shellToolkitService = hostShellToolkitService;
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
            _shellToolkitService.OutputToConsole(message, requestForceVisible);
            if (addToLog)
                _shellToolkitService.AddToLog("info", message);
        }

        void IBuildAndDeploymentLogger.LogMessage(string message)
        {
            _shellToolkitService.AddToLog("info", message);
        }

        #endregion

        private IDEConsoleLogger() { }
    }
}
