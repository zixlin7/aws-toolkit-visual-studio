using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;

using Amazon.AWSToolkit.IdentityManagement.Controller;
using Amazon.AWSToolkit.IdentityManagement.Nodes;

namespace Amazon.AWSToolkit.IdentityManagement
{
    public class IdentityManagementActivator : AbstractPluginActivator
    {
        public override string PluginName => "IdentityManagement";

        public override void RegisterMetaNodes()
        {
            var groupRootMetaNode = new IAMGroupRootViewMetaNode();
            var groupMetaNode = new IAMGroupViewMetaNode();
            groupRootMetaNode.Children.Add(groupMetaNode);

            var userRootMetaNode = new IAMUserRootViewMetaNode();
            var userMetaNode = new IAMUserViewMetaNode();
            userRootMetaNode.Children.Add(userMetaNode);

            var roleRootMetaNode = new IAMRoleRootViewMetaNode();
            var roleMetaNode = new IAMRoleViewMetaNode();
            roleRootMetaNode.Children.Add(roleMetaNode);

            var rootMetaNode = new IAMRootViewMetaNode();
            rootMetaNode.Children.Add(groupRootMetaNode);
            rootMetaNode.Children.Add(roleRootMetaNode);
            rootMetaNode.Children.Add(userRootMetaNode);
            setupContextMenuHooks(rootMetaNode);

            var accountMetaNode = ToolkitFactory.Instance.RootViewMetaNode.FindChild<AccountViewMetaNode>();
            accountMetaNode.Children.Add(rootMetaNode);
        }

        void setupContextMenuHooks(IAMRootViewMetaNode rootNode)
        {
            rootNode.IAMGroupRootViewMetaNode.OnCreateGroup =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<CreateGroupController>().Execute);

            rootNode.IAMGroupRootViewMetaNode.IAMGroupViewMetaNode.OnEdit =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<EditGroupController>().Execute);

            rootNode.IAMGroupRootViewMetaNode.IAMGroupViewMetaNode.OnDelete =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<DeleteGroupController>().Execute);

            rootNode.IAMUserRootViewMetaNode.OnCreateUser =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<CreateUserController>().Execute);

            rootNode.IAMUserRootViewMetaNode.IAMUserViewMetaNode.OnEdit =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<EditUserController>().Execute);

            rootNode.IAMUserRootViewMetaNode.IAMUserViewMetaNode.OnDelete =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<DeleteUserController>().Execute);


            rootNode.IAMRoleRootViewMetaNode.OnCreateRole =
                 new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<CreateRoleController>().Execute);

            rootNode.IAMRoleRootViewMetaNode.IAMRoleViewMetaNode.OnEdit =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<EditRoleController>().Execute);

            rootNode.IAMRoleRootViewMetaNode.IAMRoleViewMetaNode.OnDelete =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<DeleteRoleController>().Execute);

        }
    }
}
