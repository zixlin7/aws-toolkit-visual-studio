using System.Collections.Generic;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.CodeArtifact.Nodes
{
    public class CodeArtifactRootViewMetaNode : ServiceRootViewMetaNode, ICodeArtifactRootViewMetaNode
    {
        public const string CODEARTIFACT_ENDPOINT_LOOKUP = "CodeArtifact";

        public override string EndPointSystemName => CODEARTIFACT_ENDPOINT_LOOKUP;

        public DomainViewMetaNode DomainViewMetaNode => this.FindChild<DomainViewMetaNode>();

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account)
        {
            return new CodeArtifactRootViewModel(account);
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
