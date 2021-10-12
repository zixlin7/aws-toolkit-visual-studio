using System.Collections.Generic;

using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.AWSToolkit.Navigator;

namespace Amazon.AWSToolkit.RDS.Nodes
{
    public class RDSInstanceRootViewMetaNode : RDSFeatureViewMetaNode
    {

        public RDSInstanceViewMetaNode RDSInstanceViewMetaNode => this.FindChild<RDSInstanceViewMetaNode>();

        public override bool SupportsRefresh => true;

        public ActionHandlerWrapper.ActionHandler OnLaunchDBInstance
        {
            get;
            set;
        }

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("Launch Instance...", OnLaunchDBInstance, null, false,
                    typeof(AwsImageResourcePath).Assembly,
                    AwsImageResourcePath.RdsDbInstances.Path),
                null,
                new ActionHandlerWrapper("View", OnView, null, true,
                    typeof(AwsImageResourcePath).Assembly,
                    AwsImageResourcePath.RdsDbInstances.Path)
            );
    }
}
