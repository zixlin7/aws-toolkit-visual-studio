using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public SQSQueueViewMetaNode SQSViewMetaNode
        {
            get { return this.FindChild<SQSQueueViewMetaNode>(); }
        }

        public override string EndPointSystemName
        {
            get { return SQS_ENDPOINT_LOOKUP; }
        }

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

        public override IList<ActionHandlerWrapper> Actions
        {
            get
            {
                return BuildActionHandlerList(new ActionHandlerWrapper("Create Queue...", OnCreate, new ActionHandlerWrapper.ActionResponseHandler(this.OnCreateResponse), false, this.GetType().Assembly,
                    "Amazon.AWSToolkit.SimpleDB.Resources.EmbeddedImages.queue-add.png"));
            }
        }

        public override string MarketingWebSite
        {
            get
            {
                return "http://aws.amazon.com/sqs/";
            }
        }
    }
}
