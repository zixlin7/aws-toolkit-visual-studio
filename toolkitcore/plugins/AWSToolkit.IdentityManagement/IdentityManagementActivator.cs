using System;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.IdentityManagement.Controller;
using Amazon.AWSToolkit.IdentityManagement.Nodes;
using Amazon.AWSToolkit.Navigator;

namespace Amazon.AWSToolkit.IdentityManagement
{
    public class IdentityManagementActivator : AbstractPluginActivator
    {
        private IIamEntityRepository _iamEntities;

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
                new ActionHandlerWrapper.ActionHandler(new ContextCommandExecutor(() => new CreateGroupController(ToolkitContext)).Execute);

            rootNode.IAMGroupRootViewMetaNode.IAMGroupViewMetaNode.OnEdit =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<EditGroupController>().Execute);

            rootNode.IAMGroupRootViewMetaNode.IAMGroupViewMetaNode.OnDelete =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<DeleteGroupController>().Execute);

            rootNode.IAMUserRootViewMetaNode.OnCreateUser =
                new ActionHandlerWrapper.ActionHandler(new ContextCommandExecutor(() => new CreateUserController(ToolkitContext)).Execute);

            rootNode.IAMUserRootViewMetaNode.IAMUserViewMetaNode.OnEdit =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<EditUserController>().Execute);

            rootNode.IAMUserRootViewMetaNode.IAMUserViewMetaNode.OnDelete =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<DeleteUserController>().Execute);


            rootNode.IAMRoleRootViewMetaNode.OnCreateRole =
                 new ActionHandlerWrapper.ActionHandler(new ContextCommandExecutor(() => new CreateRoleController(ToolkitContext)).Execute);

            rootNode.IAMRoleRootViewMetaNode.IAMRoleViewMetaNode.OnEdit =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<EditRoleController>().Execute);

            rootNode.IAMRoleRootViewMetaNode.IAMRoleViewMetaNode.OnDelete =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<DeleteRoleController>().Execute);

        }

        public override object QueryPluginService(Type serviceType)
        {
            if (serviceType == typeof(IIamEntityRepository))
            {
                if (_iamEntities == null)
                {
                    _iamEntities = new IamEntityRepository(ToolkitContext);
                }

                return _iamEntities;
            }

            return null;
        }
    }
}
