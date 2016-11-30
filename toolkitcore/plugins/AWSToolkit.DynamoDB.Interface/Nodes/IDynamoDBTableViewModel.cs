using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.DynamoDBv2;

using Amazon.AWSToolkit.Navigator.Node;


namespace Amazon.AWSToolkit.DynamoDB.Nodes
{
    public interface IDynamoDBTableViewModel
    {
        string Table { get; }
        IAmazonDynamoDB DynamoDBClient { get; } 
    }
}
