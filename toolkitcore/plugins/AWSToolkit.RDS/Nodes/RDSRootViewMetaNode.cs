using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit;


namespace Amazon.AWSToolkit.RDS.Nodes
{
    public class RDSRootViewMetaNode : ServiceRootViewMetaNode, IRDSRootViewMetaNode
    {
        public const string RDS_ENDPOINT_LOOKUP = "RDS";

        public RDSInstanceViewMetaNode RDSInstanceViewMetaNode
        {
            get { return this.FindChild<RDSInstanceViewMetaNode>(); }
        }

        public override string EndPointSystemName
        {
            get { return RDS_ENDPOINT_LOOKUP; }
        }

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account)
        {
            return new RDSRootViewModel(account);
        }


        public override string MarketingWebSite
        {
            get
            {
                return "http://aws.amazon.com/rds/";
            }
        }

        public ActionHandlerWrapper.ActionHandler OnLaunchDBInstance
        {
            get;
            set;
        }

        private void OnLaunchDBInstanceResponse(IViewModel focus, ActionResults results)
        {
        }

        public override IList<ActionHandlerWrapper> Actions
        {
            get
            {
                return BuildActionHandlerList(new ActionHandlerWrapper("Launch Instance...",
                    OnLaunchDBInstance, new ActionHandlerWrapper.ActionResponseHandler(this.OnLaunchDBInstanceResponse), false, this.GetType().Assembly, "Amazon.AWSToolkit.RDS.Resources.EmbeddedImages.DBInstance_Launch.png"));
            }
        }

    }
}
