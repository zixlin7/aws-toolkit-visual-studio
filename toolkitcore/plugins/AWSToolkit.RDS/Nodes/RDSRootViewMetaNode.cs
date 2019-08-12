using System.Collections.Generic;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;


namespace Amazon.AWSToolkit.RDS.Nodes
{
    public class RDSRootViewMetaNode : ServiceRootViewMetaNode, IRDSRootViewMetaNode
    {
        public const string RDS_ENDPOINT_LOOKUP = "RDS";

        public RDSInstanceViewMetaNode RDSInstanceViewMetaNode => this.FindChild<RDSInstanceViewMetaNode>();

        public override string EndPointSystemName => RDS_ENDPOINT_LOOKUP;

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account)
        {
            return new RDSRootViewModel(account);
        }


        public override string MarketingWebSite => "http://aws.amazon.com/rds/";

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
                OnLaunchDBInstance, new ActionHandlerWrapper.ActionResponseHandler(this.OnLaunchDBInstanceResponse), false, this.GetType().Assembly, "Amazon.AWSToolkit.RDS.Resources.EmbeddedImages.DBInstance_Launch.png"));
    }
}
