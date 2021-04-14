using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.Navigator.Node
{
    public abstract class ServiceRootViewMetaNode : AbstractMetaNode, IServiceRootViewMetaNode
    {
        public override bool SupportsEndPoint => true;

        public abstract ServiceRootViewModel CreateServiceRootModel(AccountViewModel account, ToolkitRegion region);

        /// <summary>
        /// Before adding instances to the explorer for a region, the toolkit calls this method to 
        /// test all required data (endpoints etc) are available. For most services, only their own 
        /// endpoint is required.
        /// </summary>
        /// <param name="region">The region the explorer is about to activate</param>
        /// <param name="regionProvider">The toolkit region provider</param>
        /// <returns>True if the node hierarchy can fully support the region</returns>
        public virtual bool CanSupportRegion(ToolkitRegion region, IRegionProvider regionProvider)
        {
            return regionProvider.IsServiceAvailable(this.SdkEndpointServiceName, region.Id);
        }

        public abstract string MarketingWebSite
        {
            get;
        }
    }
}
