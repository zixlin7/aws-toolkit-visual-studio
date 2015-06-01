using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Controller
{
    public class DeleteApplicationVersionController : BaseDeleteApplicationController
    {
        public bool Execute(IAmazonElasticBeanstalk client, string applicationName, string version)
        {
            try
            {
                var desEnvRequest = new DescribeEnvironmentsRequest() { ApplicationName = applicationName, VersionLabel = version };

                if (base.TerminateEnvironments(client, desEnvRequest))
                {
                    client.DeleteApplicationVersion(new DeleteApplicationVersionRequest(){ ApplicationName = applicationName, VersionLabel = version });

                    return true;
                }
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error terminating", 
                    string.Format("Error deleting version {0} of application {1}. {2}",
                        version, applicationName, e.Message));
            }

            return false;
        }
    }
}
