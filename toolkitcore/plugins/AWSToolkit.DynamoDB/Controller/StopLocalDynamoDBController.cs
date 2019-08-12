using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.DynamoDB.Util;

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
