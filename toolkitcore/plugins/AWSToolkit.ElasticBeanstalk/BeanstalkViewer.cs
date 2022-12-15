using System;
using System.Collections.Generic;
using System.Linq;

using Amazon.AWSToolkit.Beanstalk;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.ElasticBeanstalk.Controller;
using Amazon.AWSToolkit.ElasticBeanstalk.Model;
using Amazon.AWSToolkit.ElasticBeanstalk.Models;
using Amazon.AWSToolkit.ElasticBeanstalk.Utils;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;

namespace Amazon.AWSToolkit.ElasticBeanstalk
{
    public class BeanstalkViewer : IBeanstalkViewer
    {
        private readonly ToolkitContext _toolkitContext;

        public BeanstalkViewer(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public void ViewEnvironment(string environmentName, AwsConnectionSettings connectionSettings)
        {
            Arg.NotNullOrWhitespace(environmentName, nameof(environmentName));

            IAmazonElasticBeanstalk beanstalkClient = CreateBeanstalkClient(connectionSettings);

            var model = CreateEnvironmentModel(environmentName, beanstalkClient);
            var viewController = new EnvironmentStatusController(model, _toolkitContext, connectionSettings);

            _toolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
            {
                viewController.Execute();
            });
        }

        private IAmazonElasticBeanstalk CreateBeanstalkClient(AwsConnectionSettings connectionSettings)
        {
            return _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonElasticBeanstalkClient>(
                connectionSettings.CredentialIdentifier, connectionSettings.Region);
        }

        private BeanstalkEnvironmentModel CreateEnvironmentModel(string environmentName,
            IAmazonElasticBeanstalk beanstalkClient)
        {
            return LoadEnvironmentDescription(environmentName, beanstalkClient).AsBeanstalkEnvironmentModel();
        }

        private EnvironmentDescription LoadEnvironmentDescription(string environmentName, IAmazonElasticBeanstalk beanstalkClient)
        {
            var request = new DescribeEnvironmentsRequest
            {
                EnvironmentNames = new List<string> { environmentName },
            };

            var response = beanstalkClient.DescribeEnvironments(request);
            if (response.Environments.Count != 1)
            {
                throw new BeanstalkViewerException(
                    response.Environments.Count == 0
                        ? BeanstalkViewerExceptionCode.EnvironmentNotFound
                        : BeanstalkViewerExceptionCode.TooManyEnvironments,
                    $"Failed to find Beanstalk Environment: {environmentName}");
            }

            return response.Environments.First();
        }
    }
}
