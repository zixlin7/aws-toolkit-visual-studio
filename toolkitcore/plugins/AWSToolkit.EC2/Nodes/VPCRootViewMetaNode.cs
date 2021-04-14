using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using Amazon.EC2;

namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class VPCRootViewMetaNode : ServiceRootViewMetaNode
    {
        private static readonly string EC2ServiceName = new AmazonEC2Config().RegionEndpointServiceName;


        public override string SdkEndpointServiceName => EC2ServiceName;

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account, ToolkitRegion region)
        {
            return new VPCRootViewModel(account, region);
        }

        public override string MarketingWebSite => "https://aws.amazon.com/vpc/";
    }
}
