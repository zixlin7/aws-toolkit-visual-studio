using Amazon.AWSToolkit.Account;

namespace Amazon.AWSToolkit.Navigator.Node
{
    public abstract class ServiceRootViewMetaNode : AbstractMetaNode, IServiceRootViewMetaNode
    {
        public override bool SupportsEndPoint => true;

        public abstract ServiceRootViewModel CreateServiceRootModel(AccountViewModel account);

        /// <summary>
        /// Before adding instances to the explorer for a region, the toolkit calls this method to 
        /// test all required data (endpoints etc) are available. For most services, only their own 
        /// endpoint is required.
        /// </summary>
        /// <param name="region">The region the explore is about to activate</param>
        /// <returns>True if the node hierarchy can fully support the region</returns>
        public virtual bool CanSupportRegion(RegionEndPointsManager.RegionEndPoints region)
        {
            return region.GetEndpoint(this.EndPointSystemName) != null;
        }

        public abstract string MarketingWebSite
        {
            get;
        }
    }
}
