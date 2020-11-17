using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CodeArtifact.Controller;
using Amazon.AWSToolkit.CodeArtifact.Nodes;
using Amazon.AWSToolkit.Navigator;

namespace Amazon.AWSToolkit.CodeArtifact
{
    class CodeArtifactActivator : AbstractPluginActivator
    {
        public override string PluginName => "CodeArtifact";
        public override void RegisterMetaNodes()
        {
            var domainViewMetaNode = new DomainViewMetaNode();
            var repoViewMetaNode = new RepoViewMetaNode();
            domainViewMetaNode.Children.Add(repoViewMetaNode);

            var rootViewMetaNode = new CodeArtifactRootViewMetaNode();

            rootViewMetaNode.Children.Add(domainViewMetaNode);
            setupContextMenuHooks(rootViewMetaNode);

            var accountMetaNode = ToolkitFactory.Instance.RootViewMetaNode.FindChild<AccountViewMetaNode>();
            accountMetaNode.Children.Add(rootViewMetaNode);
        }

        void setupContextMenuHooks(CodeArtifactRootViewMetaNode rootNode)
        {        
            rootNode.SelectProfile =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<SelectProfileController>().Execute);
            rootNode.DomainViewMetaNode.RepoViewMetaNode.GetRepoEndpoint =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<GetRepositoryEndpointController>().Execute);

        }
    }
}
