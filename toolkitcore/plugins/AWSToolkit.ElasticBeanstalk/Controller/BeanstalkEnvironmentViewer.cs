using System;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.ElasticBeanstalk.Nodes;
using Amazon.AWSToolkit.ElasticBeanstalk.Viewers;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Shared;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Controller
{
    public class BeanstalkEnvironmentViewer : IBeanstalkEnvironmentViewer
    {
        private readonly IAWSToolkitShellProvider _shellProvider;
        private readonly NavigatorControl _navigatorControl;

        public BeanstalkEnvironmentViewer(IAWSToolkitShellProvider shellProvider, NavigatorControl navigatorControl)
        {
            _shellProvider = shellProvider;
            _navigatorControl = navigatorControl;
        }

        public void View(string environmentName, ICredentialIdentifier identifier, ToolkitRegion region)
        {
            if (string.IsNullOrWhiteSpace(environmentName))
            {
                throw new ArgumentException($"{nameof(environmentName)} cannot be null or empty.");
            }

            if (_navigatorControl.SelectedAccount?.Identifier?.Id != identifier?.Id ||
                _navigatorControl.SelectedRegion != region)
            {
                _shellProvider.OutputToHostConsole(
                    $"Unable to find {environmentName}. You may find it in the AWS Explorer with the following credential settings: {identifier?.DisplayName}, {region?.DisplayName}");
                return;
            }

            _shellProvider.ExecuteOnUIThread(() =>
            {
                var beanstalkRootNode = _navigatorControl.SelectedAccount
                    .FindSingleChild<ElasticBeanstalkRootViewModel>(false);

                if (beanstalkRootNode == null)
                {
                    throw new NodeNotFoundException("Unable to load Beanstalk environments");
                }

                var environment = FindEnvironment(environmentName, beanstalkRootNode);

                if (environment == null)
                {
                    throw new NodeNotFoundException($"Beanstalk environment not found: {environmentName}");
                }

                _navigatorControl.SelectedNode = environment;
                environment.ExecuteDefaultAction();
            });
        }

        private static EnvironmentViewModel FindEnvironment(string environmentName, ElasticBeanstalkRootViewModel beanstalkRootNode)
        {
            var application = GetApplication(environmentName, beanstalkRootNode);

            if (application == null)
            {
                beanstalkRootNode.Refresh(false);
                application = GetApplication(environmentName, beanstalkRootNode);
            }

            if (application == null)
            {
                return null;
            }

            return GetEnvironment(environmentName, application);
        }

        private static ApplicationViewModel GetApplication(string environmentName,
            ElasticBeanstalkRootViewModel beanstalkRootNode)
        {
            return beanstalkRootNode.FindSingleChild<ApplicationViewModel>(false,
                application => GetEnvironment(environmentName, application) != null);
        }

        private static EnvironmentViewModel GetEnvironment(string environmentName, ApplicationViewModel application)
        {
            return application.FindSingleChild<EnvironmentViewModel>(false,
                environment => environment.Environment.EnvironmentName == environmentName);
        }
    }
}
