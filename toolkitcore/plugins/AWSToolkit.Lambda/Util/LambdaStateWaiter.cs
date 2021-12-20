using System.Threading.Tasks;

using Amazon.Lambda;

using log4net;

namespace Amazon.AWSToolkit.Lambda.Util
{
    public class LambdaStateWaiter
    {
        ILog Logger = LogManager.GetLogger(typeof(LambdaStateWaiter));

        private const int UpdateRequeryDelayMs = 333;
        private const int UpdateRequeryMaxTries = 300;
        private readonly IAmazonLambda _lambda;

        public LambdaStateWaiter(IAmazonLambda lambda)
        {
            _lambda = lambda;
        }

        public async Task WaitForUpdatableStateAsync(string functionName)
        {
            for (int i = 0; i < UpdateRequeryMaxTries; i++)
            {
                var configuration = await _lambda.GetFunctionConfigurationAsync(functionName);
                if (CanUpdateFunction(configuration.State, configuration.LastUpdateStatus))
                {
                    return;
                }

                await Task.Delay(UpdateRequeryDelayMs);
            }

            Logger.Warn($"Giving up waiting for Lambda function {functionName} to become updatable. The Toolkit operation waiting for this might fail.");
        }

        private bool CanUpdateFunction(State state, LastUpdateStatus lastUpdateStatus)
        {
            if (state == State.Pending)
            {
                return false;
            }

            if (lastUpdateStatus == LastUpdateStatus.InProgress)
            {
                return false;
            }

            return true;
        }
    }
}
