using System;
using System.Collections.Generic;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.EC2.Controller;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.AWSToolkit.EC2.Repositories;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Regions;
using Amazon.EC2;
using Amazon.EC2.Model;

using log4net;

namespace Amazon.AWSToolkit.EC2
{
    public class EC2Activator : AbstractPluginActivator, IAWSEC2
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(EC2Activator));

        public override string PluginName => "EC2";

        private IVpcRepository _vpcRepository;
        private IInstanceTypeRepository _instanceTypeRepository;

        public override void RegisterMetaNodes()
        {
            var rootEC2Node = registerEC2MetaNodes();
            registerVPCMetaNodes(rootEC2Node);
        }

        EC2RootViewMetaNode registerEC2MetaNodes()
        {
            var rootMetaNode = new EC2RootViewMetaNode(ToolkitContext);

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
            {
                return this;
            }

            if (serviceType == typeof(IVpcRepository))
            {
                if (_vpcRepository == null)
                {
                    _vpcRepository = new VpcRepository(ToolkitContext);
                } 

                return _vpcRepository;
            }

            if (serviceType == typeof(IInstanceTypeRepository))
            {
                if (_instanceTypeRepository == null)
                {
                    _instanceTypeRepository = new InstanceTypeRepository(ToolkitContext);
                } 

                return _instanceTypeRepository;
            }

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
                new ActionHandlerWrapper.ActionHandler(new ContextCommandExecutor(() => new LaunchController(ToolkitContext)).Execute);

            rootNode.FindChild<EC2AMIsViewMetaNode>().OnView =
                new ActionHandlerWrapper.ActionHandler(new ContextCommandExecutor(() => new ViewAMIsController(ToolkitContext)).Execute);

            EC2InstancesViewMetaNode instancesNode = rootNode.FindChild<EC2InstancesViewMetaNode>();
            instancesNode.OnLaunch =
                new ActionHandlerWrapper.ActionHandler(new ContextCommandExecutor(() => new LaunchController(ToolkitContext)).Execute);
            instancesNode.OnView =
                new ActionHandlerWrapper.ActionHandler(new ContextCommandExecutor(() => new ViewInstancesController(ToolkitContext)).Execute);

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

        bool IAWSEC2.IsVpcOnly(AccountViewModel account, ToolkitRegion region)
        {
            var ec2Client = account.CreateServiceClient<AmazonEC2Client>(region);
            return EC2Utilities.CheckForVpcOnlyMode(ec2Client);
        }

        void IAWSEC2.ConnectToInstance(AwsConnectionSettings connectionSettings, IList<string> instanceIds)
        {
            try
            {
                var controller = new ConnectToInstanceController(ToolkitContext);
                controller.Execute(connectionSettings, instanceIds);
            }
            catch (Exception e)
            {
                Logger.Error("Error connecting to instance", e);
            }
        }

        void IAWSEC2.ConnectToInstance(AwsConnectionSettings connectionSettings, string instanceId)
        {
            try
            {
                var instance = getRunningInstance(ToolkitContext, connectionSettings, instanceId);
                if (instance.IsWindowsPlatform)
                {
                    var controller = new OpenRemoteDesktopController(ToolkitContext);
                    controller.Execute(connectionSettings, instance);
                }
                else
                {
                    var controller = new OpenSSHSessionController(ToolkitContext);
                    controller.Execute(connectionSettings, instance);
                }
            }
            catch (Exception e)
            {
                Logger.Error(string.Format("Error connecting to instance {0}", instanceId), e);
                ToolkitContext.ToolkitHost.ShowError("Error Connecting", string.Format("Error connecting to instance {0}: {1}", instanceId, e.Message));
            }
        }

        private RunningInstanceWrapper getRunningInstance(ToolkitContext toolkitContext, AwsConnectionSettings connectionSettings, string instanceId)
        {
            var request = new DescribeInstancesRequest() { InstanceIds = new List<string>() { instanceId } };
            var response = toolkitContext.ServiceClientManager.CreateServiceClient<AmazonEC2Client>
                (connectionSettings.CredentialIdentifier, connectionSettings.Region).DescribeInstances(request);

            if (response.Reservations.Count != 1 && response.Reservations[0].Instances.Count != 1)
            {
                return null;
            }

            var reservation = response.Reservations[0];
            var wrapper = new RunningInstanceWrapper(reservation, reservation.Instances[0]);
            return wrapper;
        }

    #endregion
    }
}
