using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.Runtime;

namespace Amazon.AWSToolkit.SimpleDB.Nodes
{
    public class SimpleDBRootViewModel : ServiceRootViewModel, ISimpleDBRootViewModel
    {
        SimpleDBRootViewMetaNode _metaNode;
        AccountViewModel _accountViewModel;
        IAmazonSimpleDB _sdbClient;

        public SimpleDBRootViewModel(AccountViewModel accountViewModel)
            : base(accountViewModel.MetaNode.FindChild < SimpleDBRootViewMetaNode>(), accountViewModel, "Amazon SimpleDB")
        {
            this._metaNode = base.MetaNode as SimpleDBRootViewMetaNode;
            this._accountViewModel = accountViewModel;
        }

        public override string ToolTip
        {
            get
            {
                return "Amazon SimpleDB is a highly available, scalable, and flexible non-relational data store that offloads the work of database administration. Developers simply store and query data items via web services requests, and Amazon SimpleDB does the rest.";
            }
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.SimpleDB.Resources.EmbeddedImages.service-root-icon.png";
            }
        }

        protected override void BuildClient(AWSCredentials awsCredentials)
        {
            AmazonSimpleDBConfig config = new AmazonSimpleDBConfig();
            config.ServiceURL = this.CurrentEndPoint.Url;
            if (this.CurrentEndPoint.Signer != null)
                config.SignatureVersion = this.CurrentEndPoint.Signer;
            if (this.CurrentEndPoint.AuthRegion != null)
                config.AuthenticationRegion = this.CurrentEndPoint.AuthRegion;
            this._sdbClient = new AmazonSimpleDBClient(awsCredentials, config);
        }

        public IAmazonSimpleDB SimpleDBClient
        {
            get
            {
                return this._sdbClient;
            }
        }

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
                BeginCopingChildren(items);
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
                this.CurrentEndPoint.RegionSystemName, this.AccountViewModel.AccountNumber, this.Name));
        }
    }
}
