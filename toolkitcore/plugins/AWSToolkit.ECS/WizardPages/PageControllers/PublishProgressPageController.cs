using Amazon.AWSToolkit;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.Components;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ECS.Controller;
using Amazon.AWSToolkit.ECS.WizardPages.PageUI;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.ECS;
using Amazon.EC2;
using Amazon.ECS.Model;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Amazon.AWSToolkit.ECS.DeploymentWorkers;
using Amazon.ECR;
using Amazon.ElasticLoadBalancingV2;
using System.IO;
using Amazon.AWSToolkit.ECS.Nodes;

namespace Amazon.AWSToolkit.ECS.WizardPages.PageControllers
{
    public class PublishProgressPageController : IAWSWizardPageController, IDockerDeploymentHelper
    {
        private PublishProgressPage _pageUI;

        public IAWSWizard HostingWizard { get; set; }

        public string PageDescription
        {
            get
            {
                return "Please wait while we publish your project to AWS.";
            }
        }

        public string PageGroup
        {
            get { return AWSWizardConstants.DefaultPageGroup; }
        }

        public string PageID
        {
            get { return GetType().FullName; }
        }

        public string PageTitle
        {
            get
            {
                return "Publishing Container to AWS";
            }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public bool AllowShortCircuit()
        {
            return true;
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new PublishProgressPage(this);
            }

            return _pageUI;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            // we'll re-enable Back if an error occurs. Cancel (aka Close) will enable if we have
            // a successful upload and the 'auto close wizard' option has been unchecked.

            // Wizard framework currently disallows changes to this button
            //HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Cancel, false);

            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Back, false);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, false);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, false);

            PublishToAWS();
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (navigationReason == AWSWizardConstants.NavigationReason.movingBack)
                _pageUI.SetUploadFailedState(false); // toggles back to progress bar for next attenmpt

            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            // don't stand in the way of our previous sibling pages!
            return true;
        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return true;
        }

        public void TestForwardTransitionEnablement()
        {
        }

        private  void PublishToAWS()
        {
            var mode = (Constants.DeployMode) HostingWizard[PublishContainerToAWSWizardProperties.DeploymentMode];

            var account = HostingWizard[PublishContainerToAWSWizardProperties.UserAccount] as AccountViewModel;
            var region = HostingWizard[PublishContainerToAWSWizardProperties.Region] as RegionEndPointsManager.RegionEndPoints;
            var workingDirectory = HostingWizard[PublishContainerToAWSWizardProperties.SourcePath] as string;
            var configuration = HostingWizard[PublishContainerToAWSWizardProperties.Configuration] as string;

            var dockerRepository = HostingWizard[PublishContainerToAWSWizardProperties.DockerRepository] as string;
            var dockerTag = HostingWizard[PublishContainerToAWSWizardProperties.DockerTag] as string;

            bool persistSettings = false;
            if (HostingWizard[PublishContainerToAWSWizardProperties.PersistSettingsToConfigFile] is bool)
                persistSettings = (bool)HostingWizard[PublishContainerToAWSWizardProperties.PersistSettingsToConfigFile];

            var dockerImageTag = dockerRepository;
            if (!string.IsNullOrWhiteSpace(dockerTag))
                dockerImageTag += ":" + dockerTag;


            if (mode == Constants.DeployMode.PushOnly)
            {
                var state = new PushImageToECRWorker.State
                {
                    Account = account,
                    Region = region,
                    DockerImageTag = dockerImageTag,
                    Configuration = configuration,
                    WorkingDirectory = workingDirectory,

                    PersistConfigFile = persistSettings
                };

                var ecrClient = state.Account.CreateServiceClient<AmazonECRClient>(state.Region.GetEndpoint(RegionEndPointsManager.ECR_ENDPOINT_LOOKUP));

                var worker = new PushImageToECRWorker(this, ecrClient);

                ThreadPool.QueueUserWorkItem(x =>
                {
                    worker.Execute(state);
                    //this._results = worker.Results;
                }, state);
            }
            else if(mode == Constants.DeployMode.DeployToECSCluster)
            {
                var state = new DeployToClusterWorker.State
                {
                    Account = account,
                    Region = region,
                    DockerImageTag = dockerImageTag,
                    Configuration = configuration,
                    WorkingDirectory = workingDirectory,

                    TaskDefinition = HostingWizard[PublishContainerToAWSWizardProperties.TaskDefinition] as string,
                    Container = HostingWizard[PublishContainerToAWSWizardProperties.Container] as string,
                    MemoryHardLimit = HostingWizard[PublishContainerToAWSWizardProperties.MemoryHardLimit] as int?,
                    MemorySoftLimit = HostingWizard[PublishContainerToAWSWizardProperties.MemorySoftLimit] as int?,
                    PortMapping = HostingWizard[PublishContainerToAWSWizardProperties.PortMappings] as IList<PortMappingItem>,
                    EnvironmentVariables = HostingWizard[PublishContainerToAWSWizardProperties.EnvironmentVariables] as IList<EnvironmentVariableItem>,

                    SelectedRole = HostingWizard[PublishContainerToAWSWizardProperties.TaskRole] as Role,
                    SelectedManagedPolicy = HostingWizard[PublishContainerToAWSWizardProperties.TaskRoleManagedPolicy] as ManagedPolicy,

                    Cluster = HostingWizard[PublishContainerToAWSWizardProperties.Cluster] as string,
                    Service = HostingWizard[PublishContainerToAWSWizardProperties.Service] as string,
                    DesiredCount = ((int)HostingWizard[PublishContainerToAWSWizardProperties.DesiredCount]),
                    DeploymentMaximumPercent = ((int)HostingWizard[PublishContainerToAWSWizardProperties.MaximumPercent]),
                    DeploymentMinimumHealthyPercent = ((int)HostingWizard[PublishContainerToAWSWizardProperties.MinimumHealthy]),

                    PersistConfigFile = persistSettings
                };

                if (HostingWizard[PublishContainerToAWSWizardProperties.ShouldConfigureELB] is bool)
                    state.ShouldConfigureELB = (bool)HostingWizard[PublishContainerToAWSWizardProperties.ShouldConfigureELB];

                if(state.ShouldConfigureELB)
                {
                    if (HostingWizard[PublishContainerToAWSWizardProperties.CreateNewIAMRole] is bool)
                        state.CreateNewIAMRole = (bool)HostingWizard[PublishContainerToAWSWizardProperties.CreateNewIAMRole];


                    if (HostingWizard[PublishContainerToAWSWizardProperties.CreateNewLoadBalancer] is bool)
                        state.CreateNewLoadBalancer = (bool)HostingWizard[PublishContainerToAWSWizardProperties.CreateNewLoadBalancer];

                    state.LoadBalancer = HostingWizard[PublishContainerToAWSWizardProperties.LoadBalancer] as string;

                    if (HostingWizard[PublishContainerToAWSWizardProperties.CreateNewListenerPort] is bool)
                        state.CreateNewListenerPort = (bool)HostingWizard[PublishContainerToAWSWizardProperties.CreateNewListenerPort];

                    if (HostingWizard[PublishContainerToAWSWizardProperties.NewListenerPort] is int)
                        state.NewListenerPort = (int)HostingWizard[PublishContainerToAWSWizardProperties.NewListenerPort];

                    if (HostingWizard[PublishContainerToAWSWizardProperties.CreateNewTargetGroup] is bool)
                        state.CreateNewTargetGroup = (bool)HostingWizard[PublishContainerToAWSWizardProperties.CreateNewTargetGroup];


                    state.ListenerArn = HostingWizard[PublishContainerToAWSWizardProperties.ListenerArn] as string;
                    state.VpcId = HostingWizard[PublishContainerToAWSWizardProperties.VpcId] as string;
                    state.NewPathPattern = HostingWizard[PublishContainerToAWSWizardProperties.NewPathPattern] as string;
                    state.HealthCheckPath = HostingWizard[PublishContainerToAWSWizardProperties.HealthCheckPath] as string;


                }

                state.ServiceIAMRole = HostingWizard[PublishContainerToAWSWizardProperties.ServiceIAMRole] as string;
                state.TargetGroup = HostingWizard[PublishContainerToAWSWizardProperties.TargetGroup] as string;

                var ecrClient = state.Account.CreateServiceClient<AmazonECRClient>(state.Region.GetEndpoint(RegionEndPointsManager.ECR_ENDPOINT_LOOKUP));
                var ecsClient = state.Account.CreateServiceClient<AmazonECSClient>(state.Region.GetEndpoint(RegionEndPointsManager.ECS_ENDPOINT_LOOKUP));
                var elbClient = state.Account.CreateServiceClient<AmazonElasticLoadBalancingV2Client>(state.Region.GetEndpoint(RegionEndPointsManager.ELB_SERVICE_NAME));
                var iamClient = state.Account.CreateServiceClient<AmazonIdentityManagementServiceClient>(state.Region.GetEndpoint(RegionEndPointsManager.IAM_SERVICE_NAME));
                var ec2Client = state.Account.CreateServiceClient<AmazonEC2Client>(state.Region.GetEndpoint(RegionEndPointsManager.EC2_SERVICE_NAME));

                var worker = new DeployToClusterWorker(this, ecrClient, ecsClient, ec2Client, elbClient, iamClient);

                ThreadPool.QueueUserWorkItem(x =>
                {
                    worker.Execute(state);
                    //this._results = worker.Results;
                }, state);

            }

            this._pageUI.StartProgressBar();

        }

        public void AppendUploadStatus(string message, params object[] tokens)
        {
            string formattedMessage;
            try
            {
                formattedMessage = tokens.Length == 0 ? message : string.Format(message, tokens);
            }
            catch
            {
                formattedMessage = message;
            }

            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
            {
                (this as PublishProgressPageController)._pageUI.OutputProgressMessage(formattedMessage);
                ToolkitFactory.Instance.ShellProvider.UpdateStatus(formattedMessage);
                ToolkitFactory.Instance.ShellProvider.OutputToHostConsole(formattedMessage, true);
            }));
        }

        public void SendCompleteSuccessAsync(DeployToClusterWorker.State state)
        {
            AddECSTools(state.PersistConfigFile.GetValueOrDefault());
            DefaultSuccessFinish();

            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
            {
                var navigator = ToolkitFactory.Instance.Navigator;
                if (navigator.SelectedAccount != state.Account)
                    navigator.UpdateAccountSelection(new Guid(state.Account.SettingsUniqueKey), false);
                if (navigator.SelectedRegionEndPoints != state.Region)
                    navigator.UpdateRegionSelection(state.Region);

                var ecsRootNode = state.Account.Children.FirstOrDefault(x => x is RootViewModel);
                if (ecsRootNode != null)
                {
                    var clusterRootNode = ecsRootNode.Children.FirstOrDefault(x => x is ClustersRootViewModel);
                    if (clusterRootNode != null)
                    {
                        clusterRootNode.Refresh(false);

                        var clusterNode = clusterRootNode.Children.FirstOrDefault(x => x.Name == state.Cluster) as ClusterViewModel;
                        if (clusterNode != null)
                        {
                            var metaNode = clusterNode.MetaNode as ClusterViewMetaNode;
                            metaNode.OnView(clusterNode);
                        }
                    }
                }
            }));
        }

        private void AddECSTools(bool persist)
        {
            if (!persist)
                return;

            if (HostingWizard[PublishContainerToAWSWizardProperties.SelectedProjectFile] is string)
            {
                var projectFile = HostingWizard[PublishContainerToAWSWizardProperties.SelectedProjectFile] as string;
                var content = File.ReadAllText(projectFile);
                if (!content.Contains("Amazon.ECS.Tools") && content.StartsWith("<Project Sdk="))
                {
                    content = content.Replace("</Project>",
@"
  <ItemGroup>
    <DotNetCliToolReference Include=""Amazon.ECS.Tools"" Version=""0.8.0"" />
  </ItemGroup>
</Project>
"
                        );
                    File.WriteAllText(projectFile, content);
                }
            }

        }

        public void SendCompleteSuccessAsync(PushImageToECRWorker.State state)
        {
            AddECSTools(state.PersistConfigFile.GetValueOrDefault());
            DefaultSuccessFinish();

            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
            {
                var navigator = ToolkitFactory.Instance.Navigator;
                if (navigator.SelectedAccount != state.Account)
                    navigator.UpdateAccountSelection(new Guid(state.Account.SettingsUniqueKey), false);
                if (navigator.SelectedRegionEndPoints != state.Region)
                    navigator.UpdateRegionSelection(state.Region);

                var ecsRootNode = state.Account.Children.FirstOrDefault(x => x is RootViewModel);
                if (ecsRootNode != null)
                {
                    var repositoryRootNode = ecsRootNode.Children.FirstOrDefault(x => x is RepositoriesRootViewModel);
                    if (repositoryRootNode != null)
                    {
                        repositoryRootNode.Refresh(false);

                        var repositoryName = state.DockerImageTag;
                        if (repositoryName.Contains(':'))
                            repositoryName = repositoryName.Substring(0, repositoryName.IndexOf(':'));

                        var repositoryNode = repositoryRootNode.Children.FirstOrDefault(x => x.Name == repositoryName) as RepositoryViewModel;
                        if (repositoryNode != null)
                        {
                            var metaNode = repositoryNode.MetaNode as RepositoryViewMetaNode;
                            metaNode.OnView(repositoryNode);
                        }
                    }
                }
            }));
        }

        private void DefaultSuccessFinish()
        {
            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
            {
                (this as PublishProgressPageController)._pageUI.StopProgressBar();

                //var navigator = ToolkitFactory.Instance.Navigator;
                //if (navigator.SelectedAccount != state.Account)
                //    navigator.UpdateAccountSelection(new Guid(state.Account.SettingsUniqueKey), false);
                //if (navigator.SelectedRegionEndPoints != state.Region)
                //    navigator.UpdateRegionSelection(state.Region);


                HostingWizard[PublishContainerToAWSWizardProperties.WizardResult] = true;
                if (_pageUI.AutoCloseWizard)
                    HostingWizard.CancelRun();
            }));
        }

        public void SendCompleteErrorAsync(string message)
        {
            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
            {
                (this as PublishProgressPageController)._pageUI.StopProgressBar();
                (this as PublishProgressPageController)._pageUI.SetUploadFailedState(true);

                AppendUploadStatus(message);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error Uploading", message);

                // wizard framework doesn't allow this one to be changed currently
                // HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Cancel, true);

                HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Back, true);
                HostingWizard[PublishContainerToAWSWizardProperties.WizardResult] = false;

            }));
        }
    }
}
