using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit;
using Amazon.AWSToolkit.CodeCommit.Interface.Nodes;

namespace Amazon.AWSToolkit.CodeCommit.Nodes
{
    public class CodeCommitRootViewMetaNode : ServiceRootViewMetaNode, ICodeCommitRootViewMetaNode
    {
        public const string CodeCommit_ENDPOINT_LOOKUP = "CodeCommit";

        public CodeCommitRepositoryViewMetaNode CodeCommitRepositoryViewMetaNode
        {
            get { return this.FindChild<CodeCommitRepositoryViewMetaNode>(); }
        }

        public override string EndPointSystemName
        {
            get { return CodeCommit_ENDPOINT_LOOKUP; }
        }

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account)
        {
            return new CodeCommitRootViewModel(account);
        }

        public override string MarketingWebSite
        {
            get
            {
                return "http://aws.amazon.com/CodeCommit/";
            }
        }
    }
}
