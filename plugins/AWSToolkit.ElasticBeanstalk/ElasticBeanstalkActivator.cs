using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel.Design;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.ElasticBeanstalk.Nodes;
using Amazon.AWSToolkit.ElasticBeanstalk.Controller;
using Amazon.AWSToolkit.ElasticBeanstalk.Commands;

using Amazon.AWSToolkit.CommonUI;

using Amazon.AWSToolkit.CommonUI.WizardFramework;

using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;

using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;

using log4net;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.PluginServices.Deployment;

namespace Amazon.AWSToolkit.ElasticBeanstalk
{
    public class ElasticBeanstalkActivator : AbstractPluginActivator, IAWSElasticBeanstalk, IAWSToolkitDeploymentService
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ElasticBeanstalkActivator));

        public override string PluginName
        {
            get { return "Beanstalk"; }
        }

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

            return null; 
        }

        void setupContextMenuHooks(ElasticBeanstalkRootViewMetaNode rootNode)
        {
            rootNode.ApplicationViewMetaNode.OnApplicationStatus =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<ApplicationStatusController>().Execute);

            rootNode.ApplicationViewMetaNode.OnDeleteApplication =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<DeleteApplicationController>().Execute);

            rootNode.ApplicationViewMetaNode.EnvironmentViewMetaNode.OnEnvironmentStatus =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<EnvironmentStatusController>().Execute);

            rootNode.ApplicationViewMetaNode.EnvironmentViewMetaNode.OnRestartApp =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<RestartAppController>().Execute);

            rootNode.ApplicationViewMetaNode.EnvironmentViewMetaNode.OnRebuildingEnvironment =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<RebuildEnvironmentController>().Execute);

            rootNode.ApplicationViewMetaNode.EnvironmentViewMetaNode.OnTerminateEnvironment =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<TerminateEnvironmentController>().Execute);

            rootNode.ApplicationViewMetaNode.EnvironmentViewMetaNode.OnCreateConfig =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<GetConfigurationController>().Execute);
        }

        #region IAWSElasticBeanstalk Members

        IAWSToolkitDeploymentService IAWSElasticBeanstalk.DeploymentService 
        {
            get { return this as IAWSToolkitDeploymentService; }
        }

        #endregion

        #region IAWSToolkitDeploymentService Members

        string IAWSToolkitDeploymentService.DeploymentServiceIdentifier 
        {
            get { return DeploymentServiceIdentifiers.BeanstalkServiceName; } 
        }

        IEnumerable<IAWSWizardPageController> IAWSToolkitDeploymentService.ConstructDeploymentPages(IAWSWizard hostWizard, bool fastTrackRedeployment)
        {
            if (fastTrackRedeployment)
            {
                return new IAWSWizardPageController[] { new WizardPages.PageControllers.Deployment.FastTrackRepublishPageController() };
            }

            if (hostWizard.IsPropertySet(DeploymentWizardProperties.SeedData.propkey_LegacyDeploymentMode))
            {
                return new IAWSWizardPageController[]
                {
                    new WizardPages.PageControllers.LegacyDeployment.ApplicationPageController(),
                    new WizardPages.PageControllers.LegacyDeployment.EnvironmentPageController(),
                    new WizardPages.PageControllers.LegacyDeployment.AWSOptionsPageController(),
                    new WizardPages.PageControllers.LegacyDeployment.VpcOptionsPageController(),
                    new WizardPages.PageControllers.LegacyDeployment.AppOptionsPageController(),
                    new WizardPages.PageControllers.LegacyDeployment.DatabasePageController(),
                    new WizardPages.PageControllers.LegacyDeployment.PseudoReviewPageController()
                };
            }

            return new IAWSWizardPageController[]
            {
                new WizardPages.PageControllers.Deployment.StartPageController(),
                new WizardPages.PageControllers.Deployment.ApplicationPageController(),
                new WizardPages.PageControllers.Deployment.AWSOptionsPageController(),
                new WizardPages.PageControllers.Deployment.VpcOptionsPageController(),
                new WizardPages.PageControllers.Deployment.ConfigureRollingDeploymentsController(),
                new WizardPages.PageControllers.Deployment.PermissionsPageController(),
                new WizardPages.PageControllers.Deployment.ApplicationOptionsPageController(),
                new WizardPages.PageControllers.Deployment.PseudoReviewPageController()
            };
        }

        bool IAWSToolkitDeploymentService.Deploy(string deploymentPackage, IDictionary<string, object> deploymentProperties)
        {
            // keep navigator up-to-date with the final account selection in the wizard
            var selectedAccount = deploymentProperties[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as AccountViewModel;
            ToolkitFactory.Instance.Navigator.UpdateAccountSelection(new Guid(selectedAccount.SettingsUniqueKey), false);

            var command = new DeployNewApplicationCommand(deploymentPackage, deploymentProperties);
            ThreadPool.QueueUserWorkItem(command.Execute);

            return true;
        }

        bool IAWSToolkitDeploymentService.IsRedeploymentTargetValid(AccountViewModel account, string region, IDictionary<string, object> environmentDetails)
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

            var endpoint = RegionEndPointsManager.Instance.GetRegion(region)
                            .GetEndpoint(RegionEndPointsManager.ELASTICBEANSTALK_SERVICE_NAME);
            var config = new AmazonElasticBeanstalkConfig();
            config.ServiceURL = endpoint.Url;
            config.AuthenticationRegion = endpoint.AuthRegion;

            var client = new AmazonElasticBeanstalkClient(account.AccessKey, account.SecretKey, config);
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
        /// <param name="region"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        IEnumerable<ExistingServiceDeployment> IAWSToolkitDeploymentService.QueryToolkitDeployments(AccountViewModel account, string region, ILog logger)
        {
            var deployments = new List<ExistingServiceDeployment>();
            var endpoint = RegionEndPointsManager.Instance.GetRegion(region).GetEndpoint(RegionEndPointsManager.ELASTICBEANSTALK_SERVICE_NAME);
            if (endpoint == null)
                return deployments;

            try
            {

                var config = new AmazonElasticBeanstalkConfig {ServiceURL = endpoint.Url, AuthenticationRegion = endpoint.AuthRegion};
                var client = new AmazonElasticBeanstalkClient(account.AccessKey, account.SecretKey, config);

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
