using System.Collections.Generic;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.SQS.Nodes
{
    public class SQSRootViewMetaNode : ServiceRootViewMetaNode
    {
        public const string SQS_ENDPOINT_LOOKUP = "SQS";

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account)
        {
            return new SQSRootViewModel(account);
        }

        public SQSQueueViewMetaNode SQSViewMetaNode => this.FindChild<SQSQueueViewMetaNode>();

        public override string EndPointSystemName => SQS_ENDPOINT_LOOKUP;

        public ActionHandlerWrapper.ActionHandler OnCreate
        {
            get;
            set;
        }

        private void OnCreateResponse(IViewModel focus, ActionResults results)
        {
            SQSRootViewModel queueModel = focus as SQSRootViewModel;
            queueModel.AddQueue(results.Parameters[SQSActionResultsConstants.PARAM_QUEUE_URL] as string);
        }

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(new ActionHandlerWrapper("Create Queue...", OnCreate, new ActionHandlerWrapper.ActionResponseHandler(this.OnCreateResponse), false, this.GetType().Assembly,
                "Amazon.AWSToolkit.SimpleDB.Resources.EmbeddedImages.queue-add.png"));

        public override string MarketingWebSite => "https://aws.amazon.com/sqs/";
    }
}
