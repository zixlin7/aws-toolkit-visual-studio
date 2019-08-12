using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;


namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class VPCRootViewMetaNode : ServiceRootViewMetaNode
    {
        public const string EC2_ENDPOINT_LOOKUP = RegionEndPointsManager.EC2_SERVICE_NAME;


        public override string EndPointSystemName => EC2_ENDPOINT_LOOKUP;

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account)
        {
            return new VPCRootViewModel(account);
        }

        public override string MarketingWebSite => "http://aws.amazon.com/vpc/";
    }
}
