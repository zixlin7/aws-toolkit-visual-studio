using System;
using System.Collections.Generic;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Ecr;
using Amazon.AWSToolkit.ECS.Controller;
using Amazon.AWSToolkit.ECS.Model;
using Amazon.AWSToolkit.ECS.Nodes;
using Amazon.AWSToolkit.ECS.PluginServices.Ecr;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

using log4net;

namespace Amazon.AWSToolkit.ECS
{
    public class ECSActivator : AbstractPluginActivator, IAWSECS
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ECSActivator));

        public override string PluginName => "ECS";

        public bool SupportedInThisVersionOfVS()
        {
            return true;
        }

        public override void RegisterMetaNodes()
        {
            if (!SupportedInThisVersionOfVS())
                return;

            var clustersRootMetaNode = new ClustersRootViewMetaNode();
            var clusterMetaNode = new ClusterViewMetaNode();
            clustersRootMetaNode.Children.Add(clusterMetaNode);

            var taskdefsRootMetaNode = new TaskDefinitionsRootViewMetaNode();
            var taskdefMetaNode = new TaskDefinitionViewMetaNode();
            taskdefsRootMetaNode.Children.Add(taskdefMetaNode);

            var repositoriesRootMetaNode = new RepositoriesRootViewMetaNode();
            var repositoryMetaNode = new RepositoryViewMetaNode();
            repositoriesRootMetaNode.Children.Add(repositoryMetaNode);

            var rootMetaNode = new RootViewMetaNode();
            rootMetaNode.Children.Add(clustersRootMetaNode);
            //rootMetaNode.Children.Add(taskdefsRootMetaNode);
            rootMetaNode.Children.Add(repositoriesRootMetaNode);

            setupECSContextMenuHooks(rootMetaNode);

            var accountMetaNode = ToolkitFactory.Instance.RootViewMetaNode.FindChild<AccountViewMetaNode>();
            accountMetaNode.Children.Add(rootMetaNode);
        }

        public override object QueryPluginService(Type serviceType)
        {
            if (serviceType == typeof(IAWSECS))
            {
                return this;
            }

            if (serviceType == typeof(IRepositoryFactory))
            {
                return new RepositoryFactory(ToolkitContext);
            }

            if (serviceType == typeof(IEcrViewer))
            {
                return new EcrViewer(ToolkitContext);
            }

            return null;
        }

        void setupECSContextMenuHooks(RootViewMetaNode rootNode)
        {
            rootNode.OnLaunch = new CommandInstantiator<LaunchClusterController>().Execute;

            // cluster hierarchy
            var clustersRootNode = rootNode.FindChild<ClustersRootViewMetaNode>();
            clustersRootNode.OnLaunchCluster = new CommandInstantiator<LaunchClusterController>().Execute;
            var clusterNode = clustersRootNode.FindChild<ClusterViewMetaNode>();
            clusterNode.OnView = new ContextCommandExecutor(() => new ViewClusterController(ToolkitContext)).Execute;

            clusterNode.OnDelete = new CommandInstantiator<DeleteClusterController>().Execute;
            

            // taskdef hierarchy
            var taskdefsRootNode = rootNode.FindChild<TaskDefinitionsRootViewMetaNode>();
            if (taskdefsRootNode != null)
            {
                taskdefsRootNode.OnCreateTaskDefinition = new CommandInstantiator<CreateTaskDefinitionController>().Execute;
                var taskdefNode = taskdefsRootNode.FindChild<TaskDefinitionViewMetaNode>();
                taskdefNode.OnView = new CommandInstantiator<ViewTaskDefinitionController>().Execute;
            }

            // repository hierarchy
            var repositoriesRootNode = rootNode.FindChild<RepositoriesRootViewMetaNode>();
            repositoriesRootNode.OnCreateRepository = new CommandInstantiator<CreateRepositoryController>().Execute;
            var repositoryNode = repositoriesRootNode.FindChild<RepositoryViewMetaNode>();
            repositoryNode.OnView = OnViewRepository;
            repositoryNode.OnDelete = new CommandInstantiator<DeleteRepositoryController>().Execute;
        }

        private ActionResults OnViewRepository(IViewModel viewModel)
        {
            if (!(viewModel is RepositoryViewModel repositoryViewModel))
            {
                Logger.Error($"Did not receive {nameof(RepositoryViewModel)} when trying to view ECR Repo. Cancelling.");
                Logger.Error($"Received: {viewModel?.GetType().Name ?? "null"}");
                return new ActionResults().WithSuccess(false);
            }

            var model = new ViewRepositoryModel() { Repository = repositoryViewModel.Repository, };

            return new ConnectionContextCommandExecutor(() =>
                    new ViewRepositoryController(model,
                        new AwsConnectionSettings(repositoryViewModel.AccountViewModel.Identifier, repositoryViewModel.Region),
                        ToolkitContext, repositoryViewModel.ECRClient),
                ToolkitContext.ToolkitHost).Execute();
        }

        public void PublishContainerToAWS(Dictionary<string, object> seedProperties)
        {
            if (seedProperties == null)
                seedProperties = new Dictionary<string, object>();

            var controller = new PublishContainerToAWSController(ToolkitContext);
            controller.Execute(seedProperties);
        }
    }
}
