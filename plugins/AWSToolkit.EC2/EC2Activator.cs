using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.EC2;
using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.AWSToolkit.EC2.Controller;
using Amazon.EC2;

namespace Amazon.AWSToolkit.EC2
{
    public class EC2Activator : AbstractPluginActivator, IAWSEC2
    {
        public override string PluginName 
        {
            get { return "EC2"; } 
        }

        public override void RegisterMetaNodes()
        {
            var rootEC2Node = registerEC2MetaNodes();
            registerVPCMetaNodes(rootEC2Node);
        }

        EC2RootViewMetaNode registerEC2MetaNodes()
        {
            var rootMetaNode = new EC2RootViewMetaNode();

            rootMetaNode.Children.Add(new EC2AMIsViewMetaNode());
            rootMetaNode.Children.Add(new EC2InstancesViewMetaNode());
            rootMetaNode.Children.Add(new EC2KeyPairsViewMetaNode());
            rootMetaNode.Children.Add(new SecurityGroupsViewMetaNode());
            rootMetaNode.Children.Add(new EC2VolumesViewMetaNode());
            rootMetaNode.Children.Add(new ElasticIPsViewMetaNode());

            setupEC2ContextMenuHooks(rootMetaNode);

            var accountMetaNode = ToolkitFactory.Instance.RootViewMetaNode.FindChild<AccountViewMetaNode>();
            accountMetaNode.Children.Add(rootMetaNode);

            return rootMetaNode;
        }

        public override object QueryPluginService(Type serviceType)
        {
            if (serviceType == typeof(IAWSEC2))
                return this as IAWSEC2;

            return null;
        }

        void registerVPCMetaNodes(EC2RootViewMetaNode rootEC2Node)
        {
            var rootMetaNode = new VPCRootViewMetaNode();

            rootMetaNode.Children.Add(new VPCsViewMetaNode());
            rootMetaNode.Children.Add(new InternetGatewayViewMetaNode());
            rootMetaNode.Children.Add(new SubnetViewMetaNode());
            rootMetaNode.Children.Add(new RouteTableViewMetaNode());
            rootMetaNode.Children.Add(new NetworkAclViewMetaNode());

            rootMetaNode.Children.Add(rootEC2Node.FindChild<SecurityGroupsViewMetaNode>());
            rootMetaNode.Children.Add(rootEC2Node.FindChild<ElasticIPsViewMetaNode>());

            setupVPCContextMenuHooks(rootMetaNode);

            var accountMetaNode = ToolkitFactory.Instance.RootViewMetaNode.FindChild<AccountViewMetaNode>();
            accountMetaNode.Children.Add(rootMetaNode);
        }

        void setupEC2ContextMenuHooks(EC2RootViewMetaNode rootNode)
        {
            rootNode.OnLaunch =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<LaunchController>().Execute);

            rootNode.FindChild<EC2AMIsViewMetaNode>().OnView =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<ViewAMIsController>().Execute);

            EC2InstancesViewMetaNode instancesNode = rootNode.FindChild<EC2InstancesViewMetaNode>();
            instancesNode.OnLaunch =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<LaunchController>().Execute);
            instancesNode.OnView =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<ViewInstancesController>().Execute);

            rootNode.FindChild<EC2VolumesViewMetaNode>().OnView =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<ViewVolumesController>().Execute);

            rootNode.FindChild<EC2KeyPairsViewMetaNode>().OnView =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<ViewKeyPairsController>().Execute);

            rootNode.FindChild<SecurityGroupsViewMetaNode>().OnView =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<ViewSecurityGroupsController>().Execute);
        }

        void setupVPCContextMenuHooks(VPCRootViewMetaNode rootNode)
        {
            rootNode.FindChild<RouteTableViewMetaNode>().OnView =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<ViewRouteTablesController>().Execute);

            rootNode.FindChild<VPCsViewMetaNode>().OnView =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<ViewVPCsController>().Execute);

            rootNode.FindChild<InternetGatewayViewMetaNode>().OnView =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<ViewInternetGatewayController>().Execute);

            rootNode.FindChild<SubnetViewMetaNode>().OnView =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<ViewSubnetsController>().Execute);

            rootNode.FindChild<ElasticIPsViewMetaNode>().OnView =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<ViewElasticIPsController>().Execute);

            rootNode.FindChild<NetworkAclViewMetaNode>().OnView =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<ViewNetworkAclsController>().Execute);
        }

        #region IAWSEC2 implementation

        bool IAWSEC2.IsVpcOnly(IAmazonEC2 ec2Client)
        {
            return EC2Utilities.CheckForVpcOnlyMode(ec2Client);
        }

        bool IAWSEC2.IsVpcOnly(AccountViewModel account, RegionEndpoint region)
        {
            var ec2Client = new AmazonEC2Client(account.AccessKey, account.SecretKey, region);
            return EC2Utilities.CheckForVpcOnlyMode(ec2Client);
        }


        #endregion
    }
}
