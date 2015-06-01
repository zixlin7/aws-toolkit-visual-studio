using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AWSDeployment;

namespace AWSDeploymentTool
{
    class DeploymentToolCombinedObserver : DeploymentObserver
    {
        DeploymentToolConsoleObserver consoleObserver;
        DeploymentToolLogObserver logObserver;

        public DeploymentToolCombinedObserver(string logFilePath, bool verbose)
        {
            consoleObserver = new DeploymentToolConsoleObserver(verbose);
            logObserver = new DeploymentToolLogObserver(logFilePath);
        }

        public override void Status(string messageFormat, params object[] list)
        {
            consoleObserver.Status(messageFormat, list);
            logObserver.Status(messageFormat, list);
        }

        public override void Info(string messageFormat, params object[] list)
        {
            consoleObserver.Info(messageFormat, list);
            logObserver.Info(messageFormat, list);
        }

        public override void Warn(string messageFormat, params object[] list)
        {
            consoleObserver.Warn(messageFormat, list);
            logObserver.Warn(messageFormat, list);
        }

        public override void Error(string messageFormat, params object[] list)
        {
            consoleObserver.Error(messageFormat, list);
            logObserver.Error(messageFormat, list);
        }

        public override void LogOnly(string messageFormat, params object[] list)
        {
            logObserver.Info(messageFormat, list);
        }
    }
}
