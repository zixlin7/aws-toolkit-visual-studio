using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;

using Amazon.AWSToolkit.RDS.Nodes;
using Amazon.AWSToolkit.RDS.Controller;


namespace Amazon.AWSToolkit.RDS
{
    public class RDSActivator : AbstractPluginActivator
    {
        public override string PluginName => "RDS";

        public override void RegisterMetaNodes()
        {
            var instanceRootMetaNode = new RDSInstanceRootViewMetaNode();
            var instanceMetaNode = new RDSInstanceViewMetaNode();
            instanceRootMetaNode.Children.Add(instanceMetaNode);

            var subnetGroupsRootMetaNode = new RDSSubnetGroupsRootViewMetaNode();
            var subnetGroupsMetaNode = new RDSSubnetGroupViewMetaNode();
            subnetGroupsRootMetaNode.Children.Add(subnetGroupsMetaNode);

            var securityRootMetaNode = new RDSSecurityGroupRootViewMetaNode();
            var securityMetaNode = new RDSSecurityGroupViewMetaNode();
            securityRootMetaNode.Children.Add(securityMetaNode);

            var rootMetaNode = new RDSRootViewMetaNode();
            rootMetaNode.Children.Add(instanceRootMetaNode);
            rootMetaNode.Children.Add(subnetGroupsRootMetaNode);
            rootMetaNode.Children.Add(securityRootMetaNode);

            setupContextMenuHooks(rootMetaNode);

            var accountMetaNode = ToolkitFactory.Instance.RootViewMetaNode.FindChild<AccountViewMetaNode>();
            accountMetaNode.Children.Add(rootMetaNode);
        }

        void setupContextMenuHooks(RDSRootViewMetaNode rootNode)
        {
            rootNode.OnLaunchDBInstance = new ContextCommandExecutor(() => new LaunchDBInstanceController(ToolkitContext)).Execute;

            var instanceRootNode = rootNode.FindChild<RDSInstanceRootViewMetaNode>();
            instanceRootNode.OnView = new ContextCommandExecutor(() => new ViewDBInstancesController(ToolkitContext)).Execute;
            instanceRootNode.OnLaunchDBInstance = new ContextCommandExecutor(() => new LaunchDBInstanceController(ToolkitContext)).Execute;

            var instanceNode = instanceRootNode.FindChild<RDSInstanceViewMetaNode>();
            instanceNode.OnModify = new CommandInstantiator<ModifyDBInstanceController>().Execute;
            instanceNode.OnView = new ContextCommandExecutor(() => new ViewDBInstancesController(ToolkitContext)).Execute;
            if (ToolkitFactory.Instance.ShellProvider.QueryShellProviderService<IRegisterDataConnectionService>() != null)
            {
                instanceNode.OnAddToServerExplorer = new CommandInstantiator<AddToServerExplorerController>().Execute;
            }
            instanceNode.OnCreateSQLServerDatabase = new CommandInstantiator<CreateSqlServerDBController>().Execute;
            instanceNode.OnDelete = new ContextCommandExecutor(() => new DeleteDBInstanceController(ToolkitContext)).Execute;
            instanceNode.OnTakeSnapshot = new CommandInstantiator<TakeSnapshotController>().Execute;
            instanceNode.OnReboot = new CommandInstantiator<RebootInstanceController>().Execute;

            var subnetGroupsRootMetaNode = rootNode.FindChild<RDSSubnetGroupsRootViewMetaNode>();
            subnetGroupsRootMetaNode.OnView = new ContextCommandExecutor(() => new ViewDBSubnetGroupsController(ToolkitContext)).Execute;
            subnetGroupsRootMetaNode.OnCreate = new ContextCommandExecutor(() => new CreateDBSubnetGroupController(ToolkitContext)).Execute;

            var subnetGroupMetaNode = subnetGroupsRootMetaNode.FindChild<RDSSubnetGroupViewMetaNode>();
            subnetGroupMetaNode.OnView = new ContextCommandExecutor(() => new ViewDBSubnetGroupsController(ToolkitContext)).Execute;
            subnetGroupMetaNode.OnDelete = new ContextCommandExecutor(() => new DeleteSubnetGroupController(ToolkitContext)).Execute;

            var securityGroupRootMetaNode = rootNode.FindChild<RDSSecurityGroupRootViewMetaNode>();
            securityGroupRootMetaNode.OnView = new ContextCommandExecutor(() => new ViewDBSecurityGroupsController(ToolkitContext)).Execute;
            securityGroupRootMetaNode.OnCreate = new ContextCommandExecutor(() => new CreateSecurityGroupController(ToolkitContext)).Execute;

            var securityGroupMetaNode = securityGroupRootMetaNode.FindChild<RDSSecurityGroupViewMetaNode>();
            securityGroupMetaNode.OnView = new ContextCommandExecutor(() => new ViewDBSecurityGroupsController(ToolkitContext)).Execute;
            securityGroupMetaNode.OnDelete = new ContextCommandExecutor(() => new DeleteSecurityGroupController(ToolkitContext)).Execute;
        }
    }
}
