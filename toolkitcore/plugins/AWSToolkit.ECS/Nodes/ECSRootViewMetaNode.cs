using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.ECS.Nodes
{
    public class ECSRootViewMetaNode : ServiceRootViewMetaNode
    {
        public const string ECS_ENDPOINT_LOOKUP = RegionEndPointsManager.ECS_SERVICE_NAME;

        public override string EndPointSystemName
        {
            get { return ECS_ENDPOINT_LOOKUP; }
        }

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account)
        {
            return new ECSRootViewModel(account);
        }

        public ActionHandlerWrapper.ActionHandler OnLaunch
        {
            get;
            set;
        }

        public void OnLaunchResponse(IViewModel focus, ActionResults results)
        {
            ECSRootViewModel rootModel = focus as ECSRootViewModel;
        }

        public override IList<ActionHandlerWrapper> Actions
        {
            get
            {
                return BuildActionHandlerList(new ActionHandlerWrapper("Launch cluster...", 
                                                                       OnLaunch, 
                                                                       this.OnLaunchResponse, 
                                                                       false,
                                                                       this.GetType().Assembly, 
                                                                       "Amazon.AWSToolkit.ECS.Resources.EmbeddedImages.launch-cluster.png"));
            }
        }

        public override string MarketingWebSite
        {
            get
            {
                return "http://aws.amazon.com/ecs/";
            }
        }
    }
}
