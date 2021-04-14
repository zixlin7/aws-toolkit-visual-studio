using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.Navigator;

using Amazon.AWSToolkit.CloudFormation.Nodes;
using Amazon.AWSToolkit.CloudFormation.Controllers;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using log4net;
using Amazon.AWSToolkit.PluginServices.Deployment;

namespace Amazon.AWSToolkit.CloudFormation
{
    public class CloudFormationActivator : AbstractPluginActivator, IAWSCloudFormation, IAWSToolkitDeploymentService
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(CloudFormationActivator));

        private static readonly string CloudFormationServiceName =
            new AmazonCloudFormationConfig().RegionEndpointServiceName;

        public override string PluginName => "CloudFormation";

        public override void RegisterMetaNodes()
        {
            var rootMetaNode = new CloudFormationRootViewMetaNode();
            var stackMetaNode = new CloudFormationStackViewMetaNode();

            rootMetaNode.Children.Add(stackMetaNode);
            setupContextMenuHooks(rootMetaNode);

            var accountMetaNode = ToolkitFactory.Instance.RootViewMetaNode.FindChild<AccountViewMetaNode>();
            accountMetaNode.Children.Add(rootMetaNode);
        }

        public override object QueryPluginService(Type serviceType)
        {
            if (serviceType == typeof(IAWSCloudFormation))
                return this as IAWSCloudFormation;

            if (serviceType == typeof(IAWSToolkitDeploymentService))
                return this as IAWSToolkitDeploymentService;

            return null;
        }


        void setupContextMenuHooks(CloudFormationRootViewMetaNode rootNode)
        {
            rootNode.OnCreate =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<CreateStackController>().Execute);

            rootNode.CloudFormationStackViewMetaNode.OnOpen =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<ViewStackController>().Execute);

            rootNode.CloudFormationStackViewMetaNode.OnDelete =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<DeleteStackController>().Execute);
        }

        #region IAWSCloudFormation Members

        IAWSToolkitDeploymentService IAWSCloudFormation.DeploymentService => this as IAWSToolkitDeploymentService;

        public DeployedTemplateData DeployCloudFormationTemplate(string filepath, IDictionary<string, object> seedParameters)
        {
            var controller = new DeployTemplateController(ToolkitContext);
            return controller.Execute(filepath, seedParameters, new FileInfo(filepath).Name);
        }

        public DeployedTemplateData GetUrlToCostEstimate(string filepath, IDictionary<string, object> seedParameters)
        {
            var controller = new GetUrlToCostEstimateController(ToolkitContext);
            return controller.Execute(File.ReadAllText(filepath), seedParameters, new FileInfo(filepath).Name);
        }

        #endregion

        #region IAWSToolkitDeploymentService Members

        string IAWSToolkitDeploymentService.DeploymentServiceIdentifier => DeploymentServiceIdentifiers.CloudFormationServiceName;

        IEnumerable<IAWSWizardPageController> IAWSToolkitDeploymentService.ConstructDeploymentPages(IAWSWizard hostWizard, bool fastTrackRedeployment)
        {
            if (fastTrackRedeployment)
            {
                return new IAWSWizardPageController[] { new WizardPages.PageControllers.FastTrackRepublishPageController(ToolkitContext) };
            }

            return new IAWSWizardPageController[]
            {
                new WizardPages.PageControllers.AWSOptionsPageController(),
                new WizardPages.PageControllers.AppOptionsPageController(),
                new WizardPages.PageControllers.TemplateParametersController(),
                new WizardPages.PageControllers.PseudoReviewPageController()
            };
        }

        bool IAWSToolkitDeploymentService.Deploy(string deploymentPackage, IDictionary<string, object> deploymentProperties)
        {
            // keep navigator up-to-date with the final account selection in the wizard
            var selectedAccount = CommonWizardProperties.AccountSelection.GetSelectedAccount(deploymentProperties);
            ToolkitFactory.Instance.AwsConnectionManager.ChangeCredentialProvider(selectedAccount.Identifier);
            bool isRedeploy = false;
            if (deploymentProperties.ContainsKey(DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy))
                isRedeploy = (bool)deploymentProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy];

            DeploymentControllerBase command;
            if (isRedeploy)
                command = new RedeployApplicationController(deploymentPackage, deploymentProperties, ToolkitContext);
            else
                command = new DeployApplicationController(deploymentPackage, deploymentProperties, ToolkitContext);

            ThreadPool.QueueUserWorkItem(command.Execute);
            
            return true;
        }

        static readonly string[] invalidStates = new string[] 
        { 
            "ROLLBACK_IN_PROGRESS",
            "ROLLBACK_COMPLETE",
            "DELETE_IN_PROGRESS",
            "DELETE_COMPLETE"
        };

        bool IAWSToolkitDeploymentService.IsRedeploymentTargetValid(AccountViewModel account, string regionId, IDictionary<string, object> environmentDetails)
        {
            string stackName = null;
            if (environmentDetails.ContainsKey(CloudFormationConstants.DeploymentTargetQueryParam_StackName))
                stackName = environmentDetails[CloudFormationConstants.DeploymentTargetQueryParam_StackName] as string;

            if (string.IsNullOrEmpty(stackName))
            {
                throw new ArgumentException(
                    $"Expected '${CloudFormationConstants.DeploymentTargetQueryParam_StackName}' key in environmentDetails");
            }

            var region = ToolkitContext.RegionProvider.GetRegion(regionId);
            var client = account.CreateServiceClient<AmazonCloudFormationClient>(region);
            bool isValid = false;
            try
            {
                var response = client.DescribeStacks(new DescribeStacksRequest() { StackName = stackName });
                if (response.Stacks.Count != 0)
                {
                    Stack stack = response.Stacks[0];
                    isValid = !(invalidStates.Contains<string>(stack.StackStatus, StringComparer.InvariantCultureIgnoreCase));
                }
            }
            catch (Exception e) 
            {
                LOGGER.ErrorFormat("Exception while probing for existence of stack {0}: {1}", stackName, e.Message);
            }

            return isValid;
        }

        /// <summary>
        /// Returns the set of deployments from the AWS Toolkit in existence for this service
        /// </summary>
        /// <param name="account"></param>
        /// <param name="region"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        IEnumerable<ExistingServiceDeployment> IAWSToolkitDeploymentService.QueryToolkitDeployments(AccountViewModel account, string regionId, ILog logger)
        {
            var deployments = new List<ExistingServiceDeployment>();

            // test for endpoint support, just in case
            if (!ToolkitContext.RegionProvider.IsServiceAvailable(CloudFormationServiceName, regionId))
            {
                return deployments;
            }
            var region = ToolkitContext.RegionProvider.GetRegion(regionId);
            try
            {
                var client = account.CreateServiceClient<AmazonCloudFormationClient>(region);
                var response = client.DescribeStacks(new DescribeStacksRequest());
                logger.InfoFormat("Worker query for existing stacks returned {0} entries", response.Stacks.Count);
                if (response.Stacks.Count > 0)
                {
                    foreach (Stack stack in response.Stacks)
                    {
                        if (stack.StackStatus.Value.StartsWith("DELETE_", StringComparison.InvariantCultureIgnoreCase))
                        {
                            logger.InfoFormat("Worker skipping stack {0} due to status {1}", stack.StackName, stack.StackStatus);
                            continue;
                        }

                        if (stack.Outputs != null)
                        {
                            bool recognised = false;
                            foreach (Output output in stack.Outputs)
                            {
                                if (string.Compare(output.OutputKey, CloudFormationConstants.VSToolkitDeployedOuputParam, true) == 0)
                                {
                                    ExistingServiceDeployment deployment
                                        = new ExistingServiceDeployment
                                        {
                                            DeploymentService = DeploymentServiceIdentifiers.CloudFormationServiceName,
                                            DeploymentName = stack.StackName,
                                            Tag = stack
                                        };

                                    deployments.Add(deployment);
                                    logger.InfoFormat("Worker recognising stack {0} as deployed from Visual Studio", stack.StackName);
                                    recognised = true;
                                    break;
                                }
                            }

                            if (!recognised)
                                logger.InfoFormat("Worker skipping stack {0}, not recognised as deployed from Visual Studio", stack.StackName, stack.StackStatus);
                        }
                        else
                            logger.InfoFormat("Worker skipping stack {0}, no outputs available", stack.StackName);
                    }
                }
            }
            catch (Exception exc)
            {
                logger.ErrorFormat("Worker query to find existing stacks caught exception - {0}", exc.Message);
            }

            return deployments;
        }

        #endregion

    }
}
