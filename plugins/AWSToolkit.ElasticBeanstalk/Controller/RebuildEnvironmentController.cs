using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

using System.Windows.Controls.DataVisualization.Charting;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.ElasticBeanstalk.Nodes;
using Amazon.AWSToolkit.ElasticBeanstalk.View;
using Amazon.AWSToolkit.ElasticBeanstalk.Model;
using Amazon.AWSToolkit;

using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;

using log4net;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Controller
{
    public class RebuildEnvironmentController : BaseContextCommand
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(RebuildEnvironmentController));

        public override ActionResults Execute(IViewModel model)
        {
            EnvironmentViewModel environmentModel = model as EnvironmentViewModel;
            if (environmentModel == null)
                return new ActionResults().WithSuccess(false);

            string msg = string.Format(
                "Are you sure you want to rebuild the environment \"{0}\"?\r\n\r\n" +
                "Note: Rebuilding the environment may take " +
                "several minutes during which your application " +
                "will not be available. Use \"Restart App Servers\" " + 
                "if you only need to restart the application."
                , environmentModel.Name);
            if (ToolkitFactory.Instance.ShellProvider.Confirm("Rebuild Environment", msg))
            {
                var beanstalkClient = environmentModel.BeanstalkClient;

                try
                {
                    LOGGER.DebugFormat("Rebuilding environment {0}", environmentModel.Environment.EnvironmentId);
                    beanstalkClient.RebuildEnvironment(new RebuildEnvironmentRequest() { EnvironmentId = environmentModel.Environment.EnvironmentId });
                }
                catch (Exception e)
                {
                    LOGGER.Error(string.Format("Error rebuilding environment {0}", environmentModel.Environment.EnvironmentId), e);
                    ToolkitFactory.Instance.ShellProvider.ShowMessage("Error Rebuilding", "Error rebuilding environment: " + e.Message);
                    return new ActionResults().WithSuccess(false);
                }
            }

            return new ActionResults().WithSuccess(true);
        }

    }
}
