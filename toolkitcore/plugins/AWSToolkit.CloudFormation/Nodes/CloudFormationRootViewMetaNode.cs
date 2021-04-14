using System.Collections.Generic;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using Amazon.CloudFormation;

namespace Amazon.AWSToolkit.CloudFormation.Nodes
{
    public class CloudFormationRootViewMetaNode : ServiceRootViewMetaNode, ICloudFormationRootViewMetaNode
    {
        private static readonly string CloudformationServiceName = new AmazonCloudFormationConfig().RegionEndpointServiceName;

        public CloudFormationStackViewMetaNode CloudFormationStackViewMetaNode => this.FindChild<CloudFormationStackViewMetaNode>();

        public override string SdkEndpointServiceName => CloudformationServiceName;

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account, ToolkitRegion region)
        {
            return new CloudFormationRootViewModel(account, region);
        }


        public override string MarketingWebSite => "https://aws.amazon.com/cloudformation/";

        public ActionHandlerWrapper.ActionHandler OnCreate
        {
            get;
            set;
        }

        public void OnCreateResponse(IViewModel focus, ActionResults results)
        {
            var rootModel = focus as CloudFormationRootViewModel;
            var model = rootModel.AddStack(results.FocalName as string);
            if (results.RunDefaultAction)
            {
                model.ExecuteDefaultAction();
            }
        }

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(new ActionHandlerWrapper("Create Stack...", OnCreate, new ActionHandlerWrapper.ActionResponseHandler(this.OnCreateResponse), false,
                this.GetType().Assembly, "Amazon.AWSToolkit.CloudFormation.Resources.EmbeddedImages.create_stack.png"));
    }
}
