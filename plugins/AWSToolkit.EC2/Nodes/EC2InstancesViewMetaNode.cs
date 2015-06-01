using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;


namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class EC2InstancesViewMetaNode : FeatureViewMetaNode
    {
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
                return BuildActionHandlerList(
                    new ActionHandlerWrapper("Launch instance...", OnLaunch, new ActionHandlerWrapper.ActionResponseHandler(this.OnLaunchResponse), false,
                        this.GetType().Assembly, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.launch-instance.png"),
                    null,
                    new ActionHandlerWrapper("View", OnView, null, true,
                        this.GetType().Assembly, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.instance.png")
                    );
            }
        }
    }
}
