using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class EC2RootViewMetaNode : ServiceRootViewMetaNode
    {
        public const string EC2_ENDPOINT_LOOKUP = RegionEndPointsManager.EC2_SERVICE_NAME;


        public override string EndPointSystemName
        {
            get { return EC2_ENDPOINT_LOOKUP; }
        }

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account)
        {
            return new EC2RootViewModel(account);
        }

        public ActionHandlerWrapper.ActionHandler OnLaunch
        {
            get;
            set;
        }

        public void OnLaunchResponse(IViewModel focus, ActionResults results)
        {
            EC2RootViewModel rootModel = focus as EC2RootViewModel;
        }

        public override IList<ActionHandlerWrapper> Actions
        {
            get
            {
                return BuildActionHandlerList(new ActionHandlerWrapper("Launch instance...", OnLaunch, new ActionHandlerWrapper.ActionResponseHandler(this.OnLaunchResponse), false, 
                    this.GetType().Assembly, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.launch-instance.png"));
            }
        }

        public override string MarketingWebSite
        {
            get
            {
                return "http://aws.amazon.com/ec2/";
            }
        }
    }
}
