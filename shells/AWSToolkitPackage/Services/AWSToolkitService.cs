using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit;
using Amazon.AWSToolkit.VisualStudio.Shared.ServiceInterfaces;

namespace Amazon.AWSToolkit.VisualStudio.Services
{
    /// <summary>
    /// Implements the IAWSToolkit service interface, exposed as
    /// SAWSToolkitService, to give supplementary packages access
    /// to the core AWSToolkit and plugins.
    /// </summary>
    internal class AWSToolkitService : SAWSToolkitService, IAWSToolkitService
    {
        private AWSToolkitPackage _hostPackage;

        public AWSToolkitService(AWSToolkitPackage hostPackage)
        {
            _hostPackage = hostPackage;
        }

        #region IAWSToolkitService implementation

        object IAWSToolkitService.QueryAWSToolkitPluginService(Type pluginServiceType)
        {
            return ToolkitFactory.Instance.QueryPluginService(pluginServiceType);
        }

        void IAWSToolkitService.OutputToConsole(string message)
        {
            _hostPackage.OutputToConsole(message, false);
        }

        void IAWSToolkitService.OutputToConsole(string message, bool forceVisible)
        {
            _hostPackage.OutputToConsole(message, forceVisible);
        }

        public void AddToLog(string category, string message)
        {
            if (string.Compare(category, "info", true) == 0)
            {
                _hostPackage.Logger.Info(message);
                return;
            }

            if (string.Compare(category, "warn", true) == 0)
            {
                _hostPackage.Logger.Warn(message);
                return;
            }

            if (string.Compare(category, "error", true) == 0)
            {
                _hostPackage.Logger.Error(message);
                return;
            }

            if (string.Compare(category, "debug", true) == 0)
            {
                _hostPackage.Logger.Debug(message);
                return;
            }

            // don't throw it away if caller got category wrong
            _hostPackage.Logger.InfoFormat("Request to AddToLog with unknown category '{0}', message is '{1}'", category, message);
        }

        #endregion

        private AWSToolkitService() { }
    }
}
