using System.Collections.Generic;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.CloudFormation.Nodes
{
    public class CloudFormationRootViewMetaNode : ServiceRootViewMetaNode, ICloudFormationRootViewMetaNode
    {
        public const string CLOUDFORMATION_ENDPOINT_LOOKUP = "CloudFormation";

        public CloudFormationStackViewMetaNode CloudFormationStackViewMetaNode => this.FindChild<CloudFormationStackViewMetaNode>();

        public override string EndPointSystemName => CLOUDFORMATION_ENDPOINT_LOOKUP;

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account)
        {
            return new CloudFormationRootViewModel(account);
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
