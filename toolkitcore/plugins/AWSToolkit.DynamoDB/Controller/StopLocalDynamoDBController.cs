using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.DynamoDB.Nodes;
using Amazon.AWSToolkit;
using Amazon.AWSToolkit.DynamoDB.Util;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Amazon.AWSToolkit.DynamoDB.Controller
{
    public class StopLocalDynamoDBController : BaseContextCommand
    {
        public override ActionResults Execute(IViewModel model)
        {
            DynamoDBLocalManager.Instance.Stop();
            return new ActionResults().WithSuccess(true);
        }
    }
}
