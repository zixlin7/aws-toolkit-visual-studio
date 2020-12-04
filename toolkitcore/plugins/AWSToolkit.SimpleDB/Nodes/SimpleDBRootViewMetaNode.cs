using System.Collections.Generic;
using Amazon.AWSToolkit.Account;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.SimpleDB.Nodes
{
    public class SimpleDBRootViewMetaNode : ServiceRootViewMetaNode
    {
        public const string SIMPLEDB_ENDPOINT_LOOKUP = "SimpleDB";

        public override string EndPointSystemName => SIMPLEDB_ENDPOINT_LOOKUP;

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account)
        {
            return new SimpleDBRootViewModel(account);
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
