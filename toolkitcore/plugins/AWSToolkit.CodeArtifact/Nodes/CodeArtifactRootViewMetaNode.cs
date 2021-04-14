using System.Collections.Generic;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using Amazon.CodeArtifact;

namespace Amazon.AWSToolkit.CodeArtifact.Nodes
{
    public class CodeArtifactRootViewMetaNode : ServiceRootViewMetaNode, ICodeArtifactRootViewMetaNode
    {
        private static readonly string CodeArtifactServiceName = new AmazonCodeArtifactConfig().RegionEndpointServiceName;

        public override string SdkEndpointServiceName => CodeArtifactServiceName;

        public DomainViewMetaNode DomainViewMetaNode => this.FindChild<DomainViewMetaNode>();

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account, ToolkitRegion region)
        {
            return new CodeArtifactRootViewModel(account, region);
        }

        public ActionHandlerWrapper.ActionHandler SelectProfile
        {
            get;
            set;
        }

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(new ActionHandlerWrapper("Select Profile...", SelectProfile, null, false,
                this.GetType().Assembly, null));

        public override string MarketingWebSite => "https://aws.amazon.com/codeartifact/";

    }
}
