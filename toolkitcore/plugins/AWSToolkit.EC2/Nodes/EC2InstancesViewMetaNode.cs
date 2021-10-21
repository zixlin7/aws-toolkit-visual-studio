using System.Collections.Generic;

using Amazon.AWSToolkit.CommonUI.Images;
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

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("Launch instance...", OnLaunch, new ActionHandlerWrapper.ActionResponseHandler(this.OnLaunchResponse), false,
                    typeof(AwsImageResourcePath).Assembly, AwsImageResourcePath.Ec2Instances.Path),
                null,
                new ActionHandlerWrapper("View", OnView, null, true,
                    typeof(AwsImageResourcePath).Assembly, AwsImageResourcePath.Ec2Instances.Path)
            );
    }
}
