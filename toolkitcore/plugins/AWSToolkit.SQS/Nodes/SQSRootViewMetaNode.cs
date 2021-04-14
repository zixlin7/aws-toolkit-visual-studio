using System.Collections.Generic;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using Amazon.SQS;

namespace Amazon.AWSToolkit.SQS.Nodes
{
    public class SQSRootViewMetaNode : ServiceRootViewMetaNode
    {
        private static readonly string SQSServiceName = new AmazonSQSConfig().RegionEndpointServiceName;

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account, ToolkitRegion region)
        {
            return new SQSRootViewModel(account, region);
        }

        public SQSQueueViewMetaNode SQSViewMetaNode => this.FindChild<SQSQueueViewMetaNode>();

        public override string SdkEndpointServiceName => SQSServiceName;

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
