using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.ECS.Controller;
using Amazon.AWSToolkit.ECS.Nodes;
using Amazon.AWSToolkit.Navigator;

namespace Amazon.AWSToolkit.ECS
{
    public class ECSActivator : AbstractPluginActivator, IAWSECS
    {
        public override string PluginName => "ECS";

        public override void RegisterMetaNodes()
        {
            var clustersRootMetaNode = new ClustersRootViewMetaNode();
            var clusterMetaNode = new ClusterViewMetaNode();
            clustersRootMetaNode.Children.Add(clusterMetaNode);

            var repositoriesRootMetaNode = new RepositoriesRootViewMetaNode();
            var repositoryMetaNode = new RepositoryViewMetaNode();
            repositoriesRootMetaNode.Children.Add(repositoryMetaNode);

            var rootMetaNode = new RootViewMetaNode();
            rootMetaNode.Children.Add(clustersRootMetaNode);
            rootMetaNode.Children.Add(repositoriesRootMetaNode);

            setupECSContextMenuHooks(rootMetaNode);

            var accountMetaNode = ToolkitFactory.Instance.RootViewMetaNode.FindChild<AccountViewMetaNode>();
            accountMetaNode.Children.Add(rootMetaNode);
        }

        public override object QueryPluginService(Type serviceType)
        {
            if (serviceType == typeof(IAWSECS))
                return this;

            return null;
        }

        void setupECSContextMenuHooks(RootViewMetaNode rootNode)
        {
            rootNode.OnLaunch = new CommandInstantiator<LaunchClusterController>().Execute;

            // cluster hierarchy
            var clusterRootNode = rootNode.FindChild<ClustersRootViewMetaNode>();
            clusterRootNode.OnLaunchCluster = new CommandInstantiator<LaunchClusterController>().Execute;
            var clusterNode = clusterRootNode.FindChild<ClusterViewMetaNode>();
            clusterNode.OnView = new CommandInstantiator<ViewClusterController>().Execute;

            // repository hierarchy
            var repositoriesRootNode = rootNode.FindChild<RepositoriesRootViewMetaNode>();
            repositoriesRootNode.OnCreateRepository = new CommandInstantiator<CreateRepositoryController>().Execute;
            var repositoryNode = repositoriesRootNode.FindChild<RepositoryViewMetaNode>();
            repositoryNode.OnView = new CommandInstantiator<ViewRepositoryController>().Execute;
        }

        public void PublishContainerToAWS(Dictionary<string, object> seedProperties)
        {
            if (seedProperties == null)
                seedProperties = new Dictionary<string, object>();

            var controller = new PublishContainerToAWSController();
            controller.Execute(seedProperties);
        }
    }
}
