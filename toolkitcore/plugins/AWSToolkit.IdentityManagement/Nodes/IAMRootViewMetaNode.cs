using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.IdentityManagement.Nodes
{
    public class IAMRootViewMetaNode : ServiceRootViewMetaNode
    {
        public const string IAM_ENDPOINT_LOOKUP = "IAM";

        public IAMGroupRootViewMetaNode IAMGroupRootViewMetaNode
        {
            get { return this.FindChild<IAMGroupRootViewMetaNode>(); }
        }

        public IAMUserRootViewMetaNode IAMUserRootViewMetaNode
        {
            get { return this.FindChild<IAMUserRootViewMetaNode>(); }
        }

        public IAMRoleRootViewMetaNode IAMRoleRootViewMetaNode
        {
            get { return this.FindChild<IAMRoleRootViewMetaNode>(); }
        }

        public override string EndPointSystemName
        {
            get { return IAM_ENDPOINT_LOOKUP; }
        }

        public override bool SupportsRefresh
        {
            get { return true; }
        }

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account)
        {
            return new IAMRootViewModel(account);
        }

        public override string MarketingWebSite
        {
            get
            {
                return "http://aws.amazon.com/iam/";
            }
        }
    }
}
