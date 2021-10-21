using System.Collections.Generic;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using Amazon.RDS;

namespace Amazon.AWSToolkit.RDS.Nodes
{
    public class RDSRootViewMetaNode : ServiceRootViewMetaNode, IRDSRootViewMetaNode
    {
        private static readonly string RDSServiceName = new AmazonRDSConfig().RegionEndpointServiceName;

        public RDSInstanceViewMetaNode RDSInstanceViewMetaNode => this.FindChild<RDSInstanceViewMetaNode>();

        public override string SdkEndpointServiceName => RDSServiceName;

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account, ToolkitRegion region)
        {
            return new RDSRootViewModel(account, region);
        }


        public override string MarketingWebSite => "https://aws.amazon.com/rds/";

        public ActionHandlerWrapper.ActionHandler OnLaunchDBInstance
        {
            get;
            set;
        }

        private void OnLaunchDBInstanceResponse(IViewModel focus, ActionResults results)
        {
        }

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(new ActionHandlerWrapper("Launch Instance...",
                OnLaunchDBInstance, new ActionHandlerWrapper.ActionResponseHandler(this.OnLaunchDBInstanceResponse), false, typeof(AwsImageResourcePath).Assembly,
                    AwsImageResourcePath.RdsDbInstances.Path));
    }
}
