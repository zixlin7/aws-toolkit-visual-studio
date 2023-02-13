using System;
using System.Threading.Tasks;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Telemetry;

namespace Amazon.AWSToolkit.EC2.Commands
{
    /// <summary>
    /// This is a base command that can be used with EC2 operations.
    /// The command is structured such that:
    /// 1 - users are prompted for some kind of input. This is optional, and can be bypassed if not applicable.
    /// 2 - the operation is performed
    /// 3 - telemetry is logged in relation to the operation
    /// </summary>
    public abstract class BaseEc2Command : AsyncCommand
    {
        protected ToolkitContext _toolkitContext;
        protected AwsConnectionSettings _awsConnectionSettings;

        protected BaseEc2Command(AwsConnectionSettings awsConnectionSettings, ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
            _awsConnectionSettings = awsConnectionSettings;
        }

        /// <summary>
        /// The main AsyncCommand implementation. The operation takes place, and then metrics are recorded.
        /// </summary>
        protected override async Task ExecuteCoreAsync(object parameter)
        {
            var result = await PromptAndExecuteAsync(parameter);
            RecordMetric(result);
        }

        /// <summary>
        /// The main AsyncCommand handler.
        /// Users are prompted for something, and if they accept, an operation takes place.
        /// </summary>
        /// <returns>A result indicating success, failure (with exception), or cancel</returns>
        private async Task<ActionResults> PromptAndExecuteAsync(object parameter)
        {
            try
            {
                if (!await PromptAsync(parameter))
                {
                    return ActionResults.CreateCancelled();
                }

                await ExecuteAsync(parameter);

                return new ActionResults().WithSuccess(true);
            }
            catch (Exception ex)
            {
                HandleExecuteException(ex);
                return ActionResults.CreateFailed(ex);
            }
        }

        /// <summary>
        /// Prompts the user about the operation.
        /// Prompting is optional, and defaults to proceeding with the operation.
        /// When implementing a command, if you need to prompt users for input:
        /// - override this method if your prompt logic is async
        /// - override <see cref="Prompt"/> if your prompt logic is synchronous
        /// </summary>
        /// <returns>true: operation should proceed, false: operation should be cancelled</returns>
        protected virtual Task<bool> PromptAsync(object parameter)
        {
            return Task.FromResult(Prompt(parameter));
        }

        /// <summary>
        /// Prompts the user about the operation.
        /// Prompting is optional, and defaults to proceeding with the operation.
        /// When implementing a command, if you need to prompt users for input:
        /// - override <see cref="PromptAsync"/> if your prompt logic is async
        /// - override this method if your prompt logic is synchronous
        /// </summary>
        /// <returns>true: operation should proceed, false: operation should be cancelled</returns>
        protected virtual bool Prompt(object parameter)
        {
            return true;
        }

        /// <summary>
        /// The operation to take place.
        /// </summary>
        protected abstract Task ExecuteAsync(object parameter);

        /// <summary>
        /// A handler for processing when an error occurs during the command's execution.
        /// </summary>
        protected abstract void HandleExecuteException(Exception ex);

        /// <summary>
        /// A hook that allows implementing commands to record metric(s) based on the operation that was executed.
        /// This is called on completion of the command (pass, fail, or cancel).
        /// </summary>
        protected abstract void RecordMetric(ActionResults result);

        /// <summary>
        /// Utility method to create a metric object and pre-populate it with standardized fields.
        /// </summary>
        /// <typeparam name="T">Metric object to instantiate</typeparam>
        /// <param name="result">Operation result, used to populate some of the metric fields</param>
        protected T CreateMetricData<T>(ActionResults result) where T : BaseTelemetryEvent, new()
        {
            var metricData = new T();
            metricData.AwsAccount = _awsConnectionSettings?.GetAccountId(_toolkitContext.ServiceClientManager) ??
                                    MetadataValue.Invalid;
            metricData.AwsRegion = _awsConnectionSettings?.Region?.Id ?? MetadataValue.Invalid;
            metricData.Reason = TelemetryHelper.GetMetricsReason(result.Exception);

            return metricData;
        }
    }
}
