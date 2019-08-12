using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.IdentityManagement.Nodes
{
    public class IAMRootViewMetaNode : ServiceRootViewMetaNode
    {
        public const string IAM_ENDPOINT_LOOKUP = "IAM";

        public IAMGroupRootViewMetaNode IAMGroupRootViewMetaNode => this.FindChild<IAMGroupRootViewMetaNode>();

        public IAMUserRootViewMetaNode IAMUserRootViewMetaNode => this.FindChild<IAMUserRootViewMetaNode>();

        public IAMRoleRootViewMetaNode IAMRoleRootViewMetaNode => this.FindChild<IAMRoleRootViewMetaNode>();

        public override string EndPointSystemName => IAM_ENDPOINT_LOOKUP;

        public override bool SupportsRefresh => true;

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account)
        {
            return new IAMRootViewModel(account);
        }

        public override string MarketingWebSite => "http://aws.amazon.com/iam/";
    }
}
