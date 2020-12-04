using System.Collections.Generic;

//using Amazon.SimpleNotificationService.Model;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.SNS.Nodes
{
    public class SNSRootViewMetaNode : ServiceRootViewMetaNode, ISNSRootViewMetaNode
    {        
        public const string SNS_ENDPOINT_LOOKUP = "SNS";

        public SNSTopicViewMetaNode SNSTopicViewMetaNode => this.FindChild<SNSTopicViewMetaNode>();

        public override string EndPointSystemName => SNS_ENDPOINT_LOOKUP;

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account)
        {
            return new SNSRootViewModel(account);
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
