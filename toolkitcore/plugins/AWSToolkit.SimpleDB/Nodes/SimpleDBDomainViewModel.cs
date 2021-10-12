using System.Windows;
using Amazon.SimpleDB;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.CommonUI.Images;

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

        public SimpleDBRootViewModel SimpleDBRootViewModel => this._simpleDBRootViewModel;

        public IAmazonSimpleDB SimpleDBClient => this._simpleDBRootViewModel.SimpleDBClient;

        public string Domain => this._domain;

        protected override string IconName => AwsImageResourcePath.SimpleDbTable.Path;

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
            dndDataObjects.SetData(DataFormats.Text, this.Name);
            dndDataObjects.SetData("ARN", string.Format("arn:aws:sdb:{0}:{1}:domain/{2}",
                this.SimpleDBRootViewModel.Region.Id, ToolkitFactory.Instance.AwsConnectionManager.ActiveAccountId, this.Name));
        }
    }
}
