using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;

using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.SimpleDB.Nodes
{
    public class SimpleDBDomainViewModel : AbstractViewModel, ISimpleDBDomainViewModel
    {
        SimpleDBDomainViewMetaNode _metaNode;
        SimpleDBRootViewModel _simpleDBRootViewModel;
        string _domain;

        public SimpleDBDomainViewModel(SimpleDBDomainViewMetaNode metaNode, SimpleDBRootViewModel simpleDBRootViewModel, string domain)
            : base(metaNode, simpleDBRootViewModel, domain)
        {
            this._metaNode = metaNode;
            this._simpleDBRootViewModel = simpleDBRootViewModel;
            this._domain = domain;
        }

        public SimpleDBRootViewModel SimpleDBRootViewModel
        {
            get { return this._simpleDBRootViewModel; }
        }

        public IAmazonSimpleDB SimpleDBClient
        {
            get { return this._simpleDBRootViewModel.SimpleDBClient; }
        }

        public string Domain
        {
            get { return this._domain; }
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.SimpleDB.Resources.EmbeddedImages.domain-node.png";
            }
        }

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
            dndDataObjects.SetData(DataFormats.Text, this.Name);
            dndDataObjects.SetData("ARN", string.Format("arn:aws:sdb:{0}:{1}:domain/{2}",
                this.SimpleDBRootViewModel.CurrentEndPoint.RegionSystemName, this.AccountViewModel.AccountNumber, this.Name));
        }
    }
}
