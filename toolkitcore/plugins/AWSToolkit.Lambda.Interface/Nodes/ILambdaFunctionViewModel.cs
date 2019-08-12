using Amazon.AWSToolkit.Navigator.Node;
using Amazon.Lambda;

namespace Amazon.AWSToolkit.Lambda.Nodes
{
    public interface ILambdaFunctionViewModel : IViewModel
    {
        IAmazonLambda LambdaClient { get; }

        string FunctionName { get; }
        string FunctionArn { get; }
    }
}
