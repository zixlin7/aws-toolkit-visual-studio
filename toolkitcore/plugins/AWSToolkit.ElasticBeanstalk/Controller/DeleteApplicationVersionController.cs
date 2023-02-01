using System;

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
                base.TerminateEnvironments(client, desEnvRequest);
                client.DeleteApplicationVersion(new DeleteApplicationVersionRequest() { ApplicationName = applicationName, VersionLabel = version });

                return true;
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Delete Application Version Error",
                    $"Error deleting version {version} of application {applicationName}:{Environment.NewLine}{e.Message}");
            }
            return false;
        }
    }
}
