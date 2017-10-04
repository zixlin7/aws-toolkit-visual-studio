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
            var clustersRootMetaNode = new ECSClustersRootViewMetaNode();
            var clusterMetaNode = new ECSClusterViewMetaNode();
            clustersRootMetaNode.Children.Add(clusterMetaNode);

            var rootMetaNode = new ECSRootViewMetaNode();
            rootMetaNode.Children.Add(clustersRootMetaNode);

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

        void setupECSContextMenuHooks(ECSRootViewMetaNode rootNode)
        {
            rootNode.OnLaunch = new CommandInstantiator<LaunchClusterController>().Execute;

            var clusterRootNode = rootNode.FindChild<ECSClustersRootViewMetaNode>();
            clusterRootNode.OnLaunchCluster = new CommandInstantiator<LaunchClusterController>().Execute;

            var clusterNode = clusterRootNode.FindChild<ECSClusterViewMetaNode>();
            clusterNode.OnView = new CommandInstantiator<ViewClusterController>().Execute;
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
