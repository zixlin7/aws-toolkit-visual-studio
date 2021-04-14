using System.Collections.Generic;

//using Amazon.SimpleNotificationService.Model;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using Amazon.SimpleNotificationService;

namespace Amazon.AWSToolkit.SNS.Nodes
{
    public class SNSRootViewMetaNode : ServiceRootViewMetaNode, ISNSRootViewMetaNode
    {
        private static readonly string SNSServiceName = new AmazonSimpleNotificationServiceConfig().RegionEndpointServiceName;

        public SNSTopicViewMetaNode SNSTopicViewMetaNode => this.FindChild<SNSTopicViewMetaNode>();

        public override string SdkEndpointServiceName => SNSServiceName;

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account, ToolkitRegion region)
        {
            return new SNSRootViewModel(account, region);
        }

        public ActionHandlerWrapper.ActionHandler OnCreateTopic
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionHandler OnViewSubscriptions
        {
            get;
            set;
        }

        private void OnCreateTopicResponse(IViewModel focus, ActionResults results)
        {
            SNSRootViewModel rootModel = focus as SNSRootViewModel;
            string topicARN = results.Parameters["CreatedTopic"] as string;
            if (!string.IsNullOrEmpty(topicARN))
            {
                rootModel.AddTopic(topicARN);
            }
        }

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(new ActionHandlerWrapper("Create Topic...", OnCreateTopic, new ActionHandlerWrapper.ActionResponseHandler(this.OnCreateTopicResponse), false,
                    this.GetType().Assembly, "Amazon.AWSToolkit.SNS.Resources.EmbeddedImages.create_topic.png"),
                new ActionHandlerWrapper("View Subscriptions", OnViewSubscriptions, null, false,
                    this.GetType().Assembly, "Amazon.AWSToolkit.SNS.Resources.EmbeddedImages.view_subscription.png"));

        public override string MarketingWebSite => "https://aws.amazon.com/sns/";
    }
}
