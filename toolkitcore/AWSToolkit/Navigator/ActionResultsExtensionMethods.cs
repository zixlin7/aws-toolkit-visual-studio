using Amazon.AwsToolkit.Telemetry.Events.Generated;

namespace Amazon.AWSToolkit.Navigator
{
    public static class ActionResultsExtensionMethods
    {
        public static Result AsTelemetryResult(this ActionResults actionResults)
        {
            if (actionResults == null)
            {
                return Result.Failed;
            }

            if (actionResults.Success)
            {
                return Result.Succeeded;
            }

            return actionResults.Cancelled ? Result.Cancelled : Result.Failed;
        }
    }
}
