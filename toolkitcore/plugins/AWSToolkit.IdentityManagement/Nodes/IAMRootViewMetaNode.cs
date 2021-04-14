using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using Amazon.IdentityManagement;

namespace Amazon.AWSToolkit.IdentityManagement.Nodes
{
    public class IAMRootViewMetaNode : ServiceRootViewMetaNode
    {
        private static readonly string IAMServiceName = new AmazonIdentityManagementServiceConfig().RegionEndpointServiceName;

        public IAMGroupRootViewMetaNode IAMGroupRootViewMetaNode => this.FindChild<IAMGroupRootViewMetaNode>();

        public IAMUserRootViewMetaNode IAMUserRootViewMetaNode => this.FindChild<IAMUserRootViewMetaNode>();

        public IAMRoleRootViewMetaNode IAMRoleRootViewMetaNode => this.FindChild<IAMRoleRootViewMetaNode>();

        public override string SdkEndpointServiceName => IAMServiceName;

        public override bool SupportsRefresh => true;

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account, ToolkitRegion region)
        {
            return new IAMRootViewModel(account, region);
        }

        public override string MarketingWebSite => "https://aws.amazon.com/iam/";
    }
}
