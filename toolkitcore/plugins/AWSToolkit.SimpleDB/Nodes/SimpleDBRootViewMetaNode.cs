using System.Collections.Generic;
using Amazon.AWSToolkit.Account;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using Amazon.SimpleDB;

namespace Amazon.AWSToolkit.SimpleDB.Nodes
{
    public class SimpleDBRootViewMetaNode : ServiceRootViewMetaNode
    {
        private static readonly string SimpleDBServiceName = new AmazonSimpleDBConfig().RegionEndpointServiceName;

        public override string SdkEndpointServiceName => SimpleDBServiceName;

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account, ToolkitRegion region)
        {
            return new SimpleDBRootViewModel(account, region);
        }

        public SimpleDBDomainViewMetaNode SimpleDBDomainViewMetaNode => this.FindChild<SimpleDBDomainViewMetaNode>();

        public ActionHandlerWrapper.ActionHandler OnDomainCreate
        {
            get;
            set;
        }

        private void OnDomainCreateResponse(IViewModel focus, ActionResults results)
        {
            SimpleDBRootViewModel rootModel = focus as SimpleDBRootViewModel;
            rootModel.AddDomain(results.FocalName as string);
        }

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(new ActionHandlerWrapper("Create Domain...",
                OnDomainCreate, new ActionHandlerWrapper.ActionResponseHandler(this.OnDomainCreateResponse), false, this.GetType().Assembly, "Amazon.AWSToolkit.SimpleDB.Resources.EmbeddedImages.domain-create.png"));

        public override string MarketingWebSite => "https://aws.amazon.com/simpledb/";
    }
}
