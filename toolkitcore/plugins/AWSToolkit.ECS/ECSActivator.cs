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
            var rootMetaNode = new ECSRootViewMetaNode();

            rootMetaNode.Children.Add(new ECSClustersViewMetaNode());

            SetupECSContextMenuHooks(rootMetaNode);

            var accountMetaNode = ToolkitFactory.Instance.RootViewMetaNode.FindChild<AccountViewMetaNode>();
            accountMetaNode.Children.Add(rootMetaNode);
        }

        public override object QueryPluginService(Type serviceType)
        {
            if (serviceType == typeof(IAWSECS))
                return this;

            return null;
        }

        void SetupECSContextMenuHooks(ECSRootViewMetaNode rootNode)
        {
            rootNode.OnLaunch = new CommandInstantiator<LaunchClusterController>().Execute;

            ECSClustersViewMetaNode clustersNode = rootNode.FindChild<ECSClustersViewMetaNode>();
            clustersNode.OnLaunchCluster = new CommandInstantiator<LaunchClusterController>().Execute;
            clustersNode.OnView = new CommandInstantiator<ViewClustersController>().Execute;
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
