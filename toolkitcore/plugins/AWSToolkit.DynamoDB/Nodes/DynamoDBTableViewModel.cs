using System.Windows;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.DynamoDBv2;

namespace Amazon.AWSToolkit.DynamoDB.Nodes
{
    public class DynamoDBTableViewModel : AbstractViewModel, IDynamoDBTableViewModel
    {
        DynamoDBTableViewMetaNode _metaNode;
        DynamoDBRootViewModel _dynamoDBRootViewModel;
        string _table;

        public DynamoDBTableViewModel(DynamoDBTableViewMetaNode metaNode, DynamoDBRootViewModel dynamoDBRootViewModel, string table)
            : base(metaNode, dynamoDBRootViewModel, table)
        {
            this._metaNode = metaNode;
            this._dynamoDBRootViewModel = dynamoDBRootViewModel;
            this._table = table;
        }

        public DynamoDBRootViewModel DynamoDBRootViewModel => this._dynamoDBRootViewModel;

        public IAmazonDynamoDB DynamoDBClient => this._dynamoDBRootViewModel.DynamoDBClient;

        public string Table => this._table;

        protected override string IconName => "Amazon.AWSToolkit.DynamoDB.Resources.EmbeddedImages.table.png";

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
            dndDataObjects.SetData(DataFormats.Text, this.Name);
            dndDataObjects.SetData("ARN", string.Format("arn:aws:dynamodb:{0}:{1}:table/{2}",
                this.DynamoDBRootViewModel.CurrentEndPoint.RegionSystemName, this.AccountViewModel.AccountNumber, this.Name));
        }
    }
}
