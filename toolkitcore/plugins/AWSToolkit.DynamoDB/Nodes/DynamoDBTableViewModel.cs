using System;
using System.Windows;

using Amazon.AWSToolkit.CommonUI.Images;
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

        protected override string IconName => AwsImageResourcePath.DynamoDbTable.Path;

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
            try
            {
                dndDataObjects.SetData(DataFormats.Text, this.Name);
                dndDataObjects.SetData("ARN", string.Format("arn:aws:dynamodb:{0}:{1}:table/{2}",
                    this.DynamoDBRootViewModel.Region.Id, ToolkitFactory.Instance.AwsConnectionManager.ActiveAccountId, this.Name));
            }
            catch (Exception)
            {
                // Eat the error, don't destabilize the call stack
                // Don't spam the log - this event can happen frequently
            }
        }
    }
}
