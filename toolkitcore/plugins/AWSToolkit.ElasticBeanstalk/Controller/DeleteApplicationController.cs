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
    public class DeleteApplicationController : BaseDeleteApplicationController, IContextCommand
    {
        public ActionResults Execute(IViewModel model)
        {
            ApplicationViewModel appModel = model as ApplicationViewModel;
            if (appModel == null)
                return new ActionResults().WithSuccess(false);

            IAmazonElasticBeanstalk client = appModel.BeanstalkClient;
            string applicationName = appModel.Application.ApplicationName;
            return Execute(client, applicationName);
        }

        public ActionResults Execute(IAmazonElasticBeanstalk client, string applicationName)
        {
            string msg = string.Format("Are you sure you want to delete application {0}?  " +
                "This will terminate any environments and versions associated with this application. " + 
                "It also deletes any Amazon RDS DB Instances created with the environment(s). To save your data, " +
                "create a snapshot before you delete your application.", applicationName);

            if (!ToolkitFactory.Instance.ShellProvider.Confirm("Delete Application", msg))
                return new ActionResults().WithSuccess(false);

            try
            {
                var descRequest = new DescribeEnvironmentsRequest() { ApplicationName = applicationName };

                if (base.TerminateEnvironments(client, descRequest))
                {
                    client.DeleteApplication(new DeleteApplicationRequest(){ ApplicationName = applicationName });
                }
                else
                {
                    return new ActionResults().WithSuccess(false);
                }
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error terminating",
                    string.Format("Error deleting application {0}. {1}",
                        applicationName, e.Message));

                return new ActionResults().WithSuccess(false);
            }

            return new ActionResults().WithSuccess(true);
        }


    }
}
