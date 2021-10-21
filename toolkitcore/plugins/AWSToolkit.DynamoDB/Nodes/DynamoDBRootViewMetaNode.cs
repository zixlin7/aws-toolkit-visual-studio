using System.Collections.Generic;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

using Amazon.AWSToolkit.DynamoDB.Util;
using Amazon.AWSToolkit.Regions;
using Amazon.DynamoDBv2;
using Amazon.AWSToolkit.CommonUI.Images;

namespace Amazon.AWSToolkit.DynamoDB.Nodes
{
    public class DynamoDBRootViewMetaNode : ServiceRootViewMetaNode
    {
        private readonly ToolkitContext _toolkitContext;

        public DynamoDBRootViewMetaNode(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public override string SdkEndpointServiceName => DynamoDBConstants.ServiceNames.DynamoDb;

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account, ToolkitRegion region)
        {
            return new DynamoDBRootViewModel(account, region, _toolkitContext.RegionProvider);
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

            _toolkitContext.RegionProvider.SetLocalEndpoint(DynamoDBConstants.ServiceNames.DynamoDb, url);
            rootModel.UpdateDynamoDBLocalState();
        }

        private void OnDynamoDBLocalStopResponse(IViewModel focus, ActionResults results)
        {
            DynamoDBRootViewModel rootModel = focus as DynamoDBRootViewModel;
            rootModel.UpdateDynamoDBLocalState();
        }

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("Create Table...", OnTableCreate, new ActionHandlerWrapper.ActionResponseHandler(this.OnTableCreateResponse), false, typeof(AwsImageResourcePath).Assembly,
                    AwsImageResourcePath.DynamoDbTable.Path),
                new ActionHandlerWrapper("Connect to DynamoDB Local", OnStartLocal, new ActionHandlerWrapper.ActionResponseHandler(this.OnDynamoDBLocalStartResponse), false, null, null),
                new ActionHandlerWrapper("Stop DynamoDB Local", OnStopLocal, new ActionHandlerWrapper.ActionResponseHandler(this.OnDynamoDBLocalStopResponse), false, null, null));

        public override string MarketingWebSite => "https://aws.amazon.com/";
    }
}
