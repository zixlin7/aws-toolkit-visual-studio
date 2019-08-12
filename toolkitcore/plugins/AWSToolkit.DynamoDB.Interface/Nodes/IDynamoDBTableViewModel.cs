using Amazon.DynamoDBv2;


namespace Amazon.AWSToolkit.DynamoDB.Nodes
{
    public interface IDynamoDBTableViewModel
    {
        string Table { get; }
        IAmazonDynamoDB DynamoDBClient { get; } 
    }
}
