using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Beanstalk;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.ElasticBeanstalk.Commands;
using Amazon.AWSToolkit.ElasticBeanstalk.Controller;
using Amazon.AWSToolkit.ElasticBeanstalk.Models;
using Amazon.AWSToolkit.ElasticBeanstalk.Nodes;
using Amazon.AWSToolkit.ElasticBeanstalk.Utils;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.PluginServices.Deployment;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;

using log4net;

namespace Amazon.AWSToolkit.ElasticBeanstalk
{
    public class ElasticBeanstalkActivator : AbstractPluginActivator, IAWSElasticBeanstalk, IAWSToolkitDeploymentService
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ElasticBeanstalkActivator));
        private static readonly string ElasticBeanstalkServiceName = new AmazonElasticBeanstalkConfig().RegionEndpointServiceName;

        public override string PluginName => "Beanstalk";

        public override void RegisterMetaNodes()
        {
            var applicationViewMetaNode = new ApplicationViewMetaNode();
            var environmentViewMetaNode = new EnvironmentViewMetaNode();
            applicationViewMetaNode.Children.Add(environmentViewMetaNode);

            var rootMetaNode = new ElasticBeanstalkRootViewMetaNode();
            rootMetaNode.Children.Add(applicationViewMetaNode);
            setupContextMenuHooks(rootMetaNode);

            var accountMetaNode = ToolkitFactory.Instance.RootViewMetaNode.FindChild<AccountViewMetaNode>();
            accountMetaNode.Children.Add(rootMetaNode);
        }

        public override object QueryPluginService(Type serviceType) 
        {
            if (serviceType == typeof(IAWSElasticBeanstalk))
                return this as IAWSElasticBeanstalk;

            if (serviceType == typeof(IAWSToolkitDeploymentService))
                return this as IAWSToolkitDeploymentService;

            if (serviceType == typeof(IBeanstalkViewer))
            {
                return new BeanstalkViewer(ToolkitContext);
            }

            return null; 
        }

        void setupContextMenuHooks(ElasticBeanstalkRootViewMetaNode rootNode)
        {
            rootNode.ApplicationViewMetaNode.OnApplicationStatus =
                new ActionHandlerWrapper.ActionHandler(new ContextCommandExecutor(() => new ApplicationStatusController(ToolkitContext)).Execute);

            rootNode.ApplicationViewMetaNode.OnDeleteApplication =
                new ActionHandlerWrapper.ActionHandler(new ContextCommandExecutor(() => new DeleteApplicationController(ToolkitContext)).Execute);

            rootNode.ApplicationViewMetaNode.EnvironmentViewMetaNode.OnEnvironmentStatus = OnViewBeanstalkEnvironment;

            rootNode.ApplicationViewMetaNode.EnvironmentViewMetaNode.OnRestartApp = OnRestartApplication;

            rootNode.ApplicationViewMetaNode.EnvironmentViewMetaNode.OnRebuildingEnvironment = OnRebuildEnvironment;

            rootNode.ApplicationViewMetaNode.EnvironmentViewMetaNode.OnTerminateEnvironment = OnTerminateEnvironment;
        }

        private ActionResults OnViewBeanstalkEnvironment(IViewModel viewModel)
        {
            try
            {
                if (!(viewModel is EnvironmentViewModel environmentViewModel))
                {
                    throw new Exception(
                        "Unable to view Beanstalk Environment." +
                        $" Expected: {nameof(EnvironmentViewModel)}," +
                        $" Received: {viewModel?.GetType().Name ?? "null"}.");
                }

                var viewer = QueryPluginService(typeof(IBeanstalkViewer)) as IBeanstalkViewer ??
                             throw new Exception("Unable to get Beanstalk Viewer");

                viewer.ViewEnvironment(
                    environmentViewModel.Environment.EnvironmentName,
                    CreateAwsConnectionSettings(environmentViewModel));

                return new ActionResults().WithSuccess(true);
            }
            catch (Exception e)
            {
                LOGGER.Error(e.Message);
                return new ActionResults().WithSuccess(false);
            }
        }
        private ActionResults OnRestartApplication(IViewModel viewModel)
        {
            var results = RestartApplication(viewModel);

            var model = viewModel as EnvironmentViewModel;
            var connectionSettings = model?.ApplicationViewModel?
                .ElasticBeanstalkRootViewModel?.AwsConnectionSettings;

            ToolkitContext.RecordBeanstalkRestartApplication(results, connectionSettings);
            return results;

        }

        private ActionResults RestartApplication(IViewModel viewModel)
        {
            if (!(viewModel is EnvironmentViewModel environmentViewModel))
            {
                LOGGER.Error("Unable to restart Beanstalk Application." +
                             $" Expected: {nameof(EnvironmentViewModel)}," +
                             $" Received: {viewModel?.GetType().Name ?? "null"}." +
                             " Operation cancelled.");
                return new ActionResults().WithSuccess(false);
            }

            return RunBeanstalkEnvironmentCommand(environmentViewModel,
                (beanstalkEnvironment, connectionSettings) =>
                    new RestartAppController(beanstalkEnvironment, ToolkitContext, connectionSettings));
        }

        private ActionResults OnRebuildEnvironment(IViewModel viewModel)
        {
            var results = RebuildEnvironment(viewModel);

            var model = viewModel as EnvironmentViewModel;
            var connectionSettings = model?.ApplicationViewModel?
                .ElasticBeanstalkRootViewModel?.AwsConnectionSettings;

            ToolkitContext.RecordBeanstalkRebuildEnvironment(results, connectionSettings);
            return results;

        }

        private ActionResults RebuildEnvironment(IViewModel viewModel)
        {
            if (!(viewModel is EnvironmentViewModel environmentViewModel))
            {
                LOGGER.Error("Unable to rebuild Beanstalk Environment." +
                             $" Expected: {nameof(EnvironmentViewModel)}," +
                             $" Received: {viewModel?.GetType().Name ?? "null"}." +
                             " Operation cancelled.");
                return new ActionResults().WithSuccess(false);
            }

            return RunBeanstalkEnvironmentCommand(environmentViewModel,
                (beanstalkEnvironment, connectionSettings) =>
                    new RebuildEnvironmentController(beanstalkEnvironment, ToolkitContext, connectionSettings));
        }

        private ActionResults OnTerminateEnvironment(IViewModel viewModel)
        {
            var results = TerminateEnvironment(viewModel);

            var model = viewModel as EnvironmentViewModel;
            var connectionSettings = model?.ApplicationViewModel?
                .ElasticBeanstalkRootViewModel?.AwsConnectionSettings;

            ToolkitContext.RecordBeanstalkDeleteEnvironment(results, connectionSettings);
            return results;
        }

        private ActionResults TerminateEnvironment(IViewModel viewModel)
        {
            if (!(viewModel is EnvironmentViewModel environmentViewModel))
            {
                LOGGER.Error("Unable to terminate Beanstalk Environment." +
                             $" Expected: {nameof(EnvironmentViewModel)}," +
                             $" Received: {viewModel?.GetType().Name ?? "null"}." +
                             " Operation cancelled.");
                return new ActionResults().WithSuccess(false);
            }

            return RunBeanstalkEnvironmentCommand(environmentViewModel,
                (beanstalkEnvironment, connectionSettings) =>
                    new TerminateEnvironmentController(beanstalkEnvironment, ToolkitContext, connectionSettings));
        }

        private ActionResults RunBeanstalkEnvironmentCommand(EnvironmentViewModel environmentViewModel,
            Func<BeanstalkEnvironmentModel, AwsConnectionSettings, IConnectionContextCommand> commandCreator)
        {
            var beanstalkEnvironment = environmentViewModel.Environment.AsBeanstalkEnvironmentModel();
            var connectionSettings = CreateAwsConnectionSettings(environmentViewModel);

            return new ConnectionContextCommandExecutor(
                () => commandCreator(beanstalkEnvironment, connectionSettings),
                ToolkitContext.ToolkitHost
            ).Execute();
        }

        private static AwsConnectionSettings CreateAwsConnectionSettings(EnvironmentViewModel beanstalkEnvironment)
        {
            var region = beanstalkEnvironment.ApplicationViewModel.ElasticBeanstalkRootViewModel.Region;
            var connectionSettings = new AwsConnectionSettings(beanstalkEnvironment.AccountViewModel.Identifier, region);
            return connectionSettings;
        }

        #region IAWSElasticBeanstalk Members

        IAWSToolkitDeploymentService IAWSElasticBeanstalk.DeploymentService => this as IAWSToolkitDeploymentService;

        #endregion

        #region IAWSToolkitDeploymentService Members

        string IAWSToolkitDeploymentService.DeploymentServiceIdentifier => DeploymentServiceIdentifiers.BeanstalkServiceName;

        IEnumerable<IAWSWizardPageController> IAWSToolkitDeploymentService.ConstructDeploymentPages(IAWSWizard hostWizard, bool fastTrackRedeployment)
        {
            if (fastTrackRedeployment)
            {
                return new IAWSWizardPageController[] { new FastTrackRepublishPageController(ToolkitContext) };
            }

            var isCoreCLRProject = false;
            if (hostWizard.IsPropertySet(DeploymentWizardProperties.SeedData.propkey_ProjectType))
            {
                isCoreCLRProject = (hostWizard.GetProperty(DeploymentWizardProperties.SeedData.propkey_ProjectType) as string)
                                        .Equals(DeploymentWizardProperties.NetCoreWebProject, StringComparison.OrdinalIgnoreCase);
            }

            return new IAWSWizardPageController[]
            {
                new StartPageController(ToolkitContext),
                new ApplicationPageController(),
                new AWSOptionsPageController(),
                new VpcOptionsPageController(),
                new ConfigureRollingDeploymentsController(),
                new PermissionsPageController(),
                isCoreCLRProject ? new CoreCLRApplicationOptionsPageController() as IAWSWizardPageController
                                 : new ApplicationOptionsPageController(),
                new PseudoReviewPageController()
            };
        }

        bool IAWSToolkitDeploymentService.Deploy(string deploymentPackage, IDictionary<string, object> deploymentProperties)
        {
            // keep navigator up-to-date with the final account/region selection in the wizard
            var selectedAccount = CommonWizardProperties.AccountSelection.GetSelectedAccount(deploymentProperties);
            var selectedRegion =  CommonWizardProperties.AccountSelection.GetSelectedRegion(deploymentProperties);
            var navigator = ToolkitFactory.Instance.Navigator;
            if (navigator.SelectedAccount != selectedAccount || navigator.SelectedRegion != selectedRegion)
            {
                ToolkitContext.ConnectionManager.ChangeConnectionSettings(selectedAccount?.Identifier, selectedRegion);
            }

            // If the deployment package is a directory then that indicates we have the project location and we still need to build the project.
            // In that case switch to use Amazon.ElasticBeanstalk.Tools to do the combined build and deployment. This is currently just done
            // for Linux .NET Core deployments.
            if (Directory.Exists(deploymentPackage))
            {
                var command = new DeployWithEbToolsCommand(deploymentPackage, deploymentProperties);
                ThreadPool.QueueUserWorkItem(command.Execute);
            }
            else
            {
                var command = new DeployNewApplicationCommand(deploymentPackage, deploymentProperties, ToolkitContext);
                ThreadPool.QueueUserWorkItem(command.Execute);
            }

            return true;
        }

        bool IAWSToolkitDeploymentService.IsRedeploymentTargetValid(AccountViewModel account, string regionId, IDictionary<string, object> environmentDetails)
        {
            string applicationName = null;
            string environmentName = null;

            if (environmentDetails.ContainsKey(BeanstalkConstants.DeploymentTargetQueryParam_ApplicationName))
                applicationName = environmentDetails[BeanstalkConstants.DeploymentTargetQueryParam_ApplicationName] as string;
            if (environmentDetails.ContainsKey(BeanstalkConstants.DeploymentTargetQueryParam_EnvironmentName))
                environmentName = environmentDetails[BeanstalkConstants.DeploymentTargetQueryParam_EnvironmentName] as string;

            if (string.IsNullOrEmpty(applicationName) || string.IsNullOrEmpty(environmentName))
                throw new ArgumentException(string.Format("Expected to find '{0}' and '{1}' keys in environmentDetails",
                                                            BeanstalkConstants.DeploymentTargetQueryParam_ApplicationName,
                                                            BeanstalkConstants.DeploymentTargetQueryParam_EnvironmentName));

            var region = ToolkitContext.RegionProvider.GetRegion(regionId);

            var client = account.CreateServiceClient<AmazonElasticBeanstalkClient>(region);
            bool isValid = false;
            try
            {
                var response = client.DescribeEnvironments(new DescribeEnvironmentsRequest()
                {
                    ApplicationName = applicationName,
                    EnvironmentNames = new List<string>() { environmentName },
                    IncludeDeleted = false
                });

                if (response.Environments.Count != 0)
                    isValid = !(response.Environments[0].Status.Value
                                        .StartsWith("Terminat", StringComparison.InvariantCultureIgnoreCase));
            }
            catch (Exception e)
            {
                LOGGER.ErrorFormat("Exception while probing for existence of application environment {0} {1}: {2}", 
                                    applicationName, environmentName, e.Message);
            }

            return isValid;
        }

        /// <summary>
        /// Returns the set of AWS Toolkit deployments in existence for this service
        /// </summary>
        /// <param name="account"></param>
        /// <param name="regionId"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        IEnumerable<ExistingServiceDeployment> IAWSToolkitDeploymentService.QueryToolkitDeployments(AccountViewModel account, string regionId, ILog logger)
        {
            var deployments = new List<ExistingServiceDeployment>();
            if (!ToolkitContext.RegionProvider.IsServiceAvailable(ElasticBeanstalkServiceName, regionId))
            {
                return deployments;
            }

            try
            {
                var region = ToolkitContext.RegionProvider.GetRegion(regionId);
                var client = account.CreateServiceClient<AmazonElasticBeanstalkClient>(region);

                var response = client.DescribeApplications(new DescribeApplicationsRequest());
                foreach (var application in response.Applications)
                {
                    var deployment
                        = new ExistingServiceDeployment
                        {
                            DeploymentService = DeploymentServiceIdentifiers.BeanstalkServiceName,
                            DeploymentName = application.ApplicationName,
                            Tag = null
                        };

                    deployments.Add(deployment);
                }
            }
            catch (Exception exc)
            {
                logger.Error(GetType().FullName + ", exception in Worker whilst querying Elastic Beanstalk applications", exc);
            }

            return deployments;
        }

        #endregion
    }
}
