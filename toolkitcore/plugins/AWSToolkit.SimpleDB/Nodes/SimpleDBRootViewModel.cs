using System;
using System.Collections.Generic;
using System.Windows;
using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using Amazon.Runtime;

namespace Amazon.AWSToolkit.SimpleDB.Nodes
{
    public class SimpleDBRootViewModel : ServiceRootViewModel, ISimpleDBRootViewModel
    {
        private readonly SimpleDBRootViewMetaNode _metaNode;
        private readonly Lazy<IAmazonSimpleDB> _sdbClient;

        public SimpleDBRootViewModel(AccountViewModel accountViewModel, ToolkitRegion region)
            : base(accountViewModel.MetaNode.FindChild < SimpleDBRootViewMetaNode>(), accountViewModel, "Amazon SimpleDB", region)
        {
            _metaNode = base.MetaNode as SimpleDBRootViewMetaNode;
            _sdbClient = new Lazy<IAmazonSimpleDB>(CreateSimpleDbClient);
        }

        public override string ToolTip => "Amazon SimpleDB is a highly available, scalable, and flexible non-relational data store that offloads the work of database administration. Developers simply store and query data items via web services requests, and Amazon SimpleDB does the rest.";

        protected override string IconName => "Amazon.AWSToolkit.SimpleDB.Resources.EmbeddedImages.service-root-icon.png";

        public IAmazonSimpleDB SimpleDBClient => this._sdbClient.Value;

        protected override void LoadChildren()
        {
            try
            {
                List<IViewModel> items = new List<IViewModel>();
                ListDomainsResponse response = new ListDomainsResponse();
                do
                {
                    var request = new ListDomainsRequest() { NextToken = response.NextToken };
                    ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)request).AddBeforeRequestHandler(Constants.AWSExplorerDescribeUserAgentRequestEventHandler);
                    response = this.SimpleDBClient.ListDomains(request);

                    foreach (string name in response.DomainNames)
                    {
                        var child = new SimpleDBDomainViewModel(this._metaNode.SimpleDBDomainViewMetaNode, this, name);
                        items.Add(child);
                    }
                } while (!string.IsNullOrEmpty(response.NextToken));

                items.Sort(new Comparison<IViewModel>(AWSViewModel.CompareViewModel));
                SetChildren(items);
            }
            catch (Exception e)
            {
                AddErrorChild(e);
            }
        }

        internal void AddDomain(string domainName)
        {
            SimpleDBDomainViewModel viewModel = new SimpleDBDomainViewModel(this._metaNode.SimpleDBDomainViewMetaNode, this, domainName);
            AddChild(viewModel);
        }

        internal void RemoveDomain(string domainName)
        {
            this.RemoveChild(domainName);
        }

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
            dndDataObjects.SetData("ARN", string.Format("arn:aws:sdb:{0}:{1}:*",
                this.Region.Id, ToolkitFactory.Instance.AwsConnectionManager.ActiveAccountId, this.Name));
        }

        private IAmazonSimpleDB CreateSimpleDbClient()
        {
            return AccountViewModel.CreateServiceClient<AmazonSimpleDBClient>(Region);
        }
    }
}
