using System.Collections.Generic;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using Amazon.EC2;

namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class EC2RootViewMetaNode : ServiceRootViewMetaNode
    {
        private static readonly string EC2ServiceName = new AmazonEC2Config().RegionEndpointServiceName;
        private readonly ToolkitContext _toolkitContext;

        public EC2RootViewMetaNode(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public override string SdkEndpointServiceName => EC2ServiceName;

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account, ToolkitRegion region)
        {
            return new EC2RootViewModel(_toolkitContext, account, region);
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

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(new ActionHandlerWrapper("Launch instance...", OnLaunch, new ActionHandlerWrapper.ActionResponseHandler(this.OnLaunchResponse), false,
                typeof(AwsImageResourcePath).Assembly, AwsImageResourcePath.Ec2Instances.Path));

        public override string MarketingWebSite => "https://aws.amazon.com/ec2/";
    }
}
