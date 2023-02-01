using System.Collections.Generic;
using System.Text;

using Amazon.AWSToolkit.Exceptions;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;

using log4net;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Controller
{
    public abstract class BaseDeleteApplicationController
    {
        protected static ILog _logger = LogManager.GetLogger(typeof(BaseDeleteApplicationController));

        protected void TerminateEnvironments(IAmazonElasticBeanstalk client, DescribeEnvironmentsRequest descRequest)
        {
            var desEnvResponse = client.DescribeEnvironments(descRequest);

            var envNotDeletable = new List<string>();
            foreach (var env in desEnvResponse.Environments)
            {
                if (BeanstalkConstants.STATUS_LAUNCHING.Equals(env.Status) ||
                    BeanstalkConstants.STATUS_TERMINATING.Equals(env.Status) ||
                    BeanstalkConstants.STATUS_UPDATING.Equals(env.Status))
                {
                    envNotDeletable.Add(env.EnvironmentName);
                }
            }

            if (envNotDeletable.Count == 0)
            {
                foreach (var env in desEnvResponse.Environments)
                {
                    if (BeanstalkConstants.STATUS_READY.Equals(env.Status))
                    {
                        client.TerminateEnvironment(new TerminateEnvironmentRequest() { EnvironmentId = env.EnvironmentId });
                    }
                }
            }
            else
            {
                var msg = new StringBuilder();
                msg.AppendLine("Please wait for the following environment(s) to finish launching or updating:\r\n");
                foreach (var env in envNotDeletable)
                {
                    msg.AppendLine("\t" + env);
                }

                throw new ToolkitException(msg.ToString(), ToolkitException.CommonErrorCode.UnsupportedState);
            }
        }
    }
}
