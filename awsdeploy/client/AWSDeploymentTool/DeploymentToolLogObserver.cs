using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using AWSDeployment;

using log4net;
using log4net.Config;
using log4net.Appender;
using log4net.Layout;

namespace AWSDeploymentTool
{
    internal class DeploymentToolLogObserver : DeploymentObserver
    {
        private static ILog LOGGER = LogManager.GetLogger(typeof(CloudFormationDeploymentEngine));

        public DeploymentToolLogObserver(string logFilePath)
        {
            if (!string.IsNullOrEmpty(logFilePath))
            {
                FileAppender fileAppender = new FileAppender();

                if (!Path.IsPathRooted(logFilePath))
                    logFilePath = Environment.CurrentDirectory + Path.DirectorySeparatorChar + logFilePath;

                fileAppender.File = logFilePath;

                var layout = new SimpleLayout();
                layout.ActivateOptions();
                fileAppender.Layout = layout;

                if (File.Exists(logFilePath))
                    fileAppender.AppendToFile = true;
                else
                    fileAppender.AppendToFile = false;

                fileAppender.ActivateOptions();
                BasicConfigurator.Configure(fileAppender);
            }
            else
                throw new Exception("Can't create logfile with empty path");
        }

        public override void Status(string messageFormat, params object[] list)
        {
            LOGGER.InfoFormat(messageFormat, list);
        }

        public override void Info(string messageFormat, params object[] list)
        {
            LOGGER.InfoFormat(messageFormat, list);
        }

        public override void Warn(string messageFormat, params object[] list) 
        {
            LOGGER.WarnFormat(messageFormat, list);
        }

        public override void Error(string messageFormat, params object[] list) 
        {
            LOGGER.ErrorFormat(messageFormat, list);
        }

        public override void LogOnly(string messageFormat, params object[] list) 
        {
            LOGGER.InfoFormat(messageFormat, list);
        }
    }
}
