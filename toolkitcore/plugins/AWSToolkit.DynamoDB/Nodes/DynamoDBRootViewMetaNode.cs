using System.Collections.Generic;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

using Amazon.AWSToolkit.DynamoDB.Util;

namespace Amazon.AWSToolkit.DynamoDB.Nodes
{
    public class DynamoDBRootViewMetaNode : ServiceRootViewMetaNode
    {
        public const string DYNAMODB_ENDPOINT_LOOKUP = "DynamoDB";

        public override string EndPointSystemName => DYNAMODB_ENDPOINT_LOOKUP;

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account)
        {
            return new DynamoDBRootViewModel(account);
        }

        public DynamoDBTableViewMetaNode DynamoDBTableViewMetaNode => this.FindChild<DynamoDBTableViewMetaNode>();

        public ActionHandlerWrapper.ActionHandler OnTableCreate
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionHandler OnStartLocal
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionHandler OnStopLocal
        {
            get;
            set;
        }

        private void OnTableCreateResponse(IViewModel focus, ActionResults results)
        {
            DynamoDBRootViewModel rootModel = focus as DynamoDBRootViewModel;
            rootModel.AddTable (results.FocalName as string);
        }

        private void OnDynamoDBLocalStartResponse(IViewModel focus, ActionResults results)
        {
            DynamoDBRootViewModel rootModel = focus as DynamoDBRootViewModel;
            string url = string.Format("http://localhost:{0}", DynamoDBLocalManager.Instance.LastConfiguredPort);
            RegionEndPointsManager.GetInstance().LocalRegion.RegisterEndPoint(DynamoDBRootViewMetaNode.DYNAMODB_ENDPOINT_LOOKUP, url);
            rootModel.UpdateDynamoDBLocalState();
        }

        private void OnDynamoDBLocalStopResponse(IViewModel focus, ActionResults results)
        {
            DynamoDBRootViewModel rootModel = focus as DynamoDBRootViewModel;
            rootModel.UpdateDynamoDBLocalState();
        }

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("Create Table...", OnTableCreate, new ActionHandlerWrapper.ActionResponseHandler(this.OnTableCreateResponse), false, this.GetType().Assembly, "Amazon.AWSToolkit.DynamoDB.Resources.EmbeddedImages.table_add.png"),
                new ActionHandlerWrapper("Connect to DynamoDB Local", OnStartLocal, new ActionHandlerWrapper.ActionResponseHandler(this.OnDynamoDBLocalStartResponse), false, null, null),
                new ActionHandlerWrapper("Stop DynamoDB Local", OnStopLocal, new ActionHandlerWrapper.ActionResponseHandler(this.OnDynamoDBLocalStopResponse), false, null, null));

        public override string MarketingWebSite => "http://aws.amazon.com/";
    }
}
