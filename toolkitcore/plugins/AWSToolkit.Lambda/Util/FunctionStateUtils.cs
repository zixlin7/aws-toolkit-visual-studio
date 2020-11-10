using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Lambda.Controller;
using Amazon.Lambda;
using log4net;


namespace Amazon.AWSToolkit.Lambda.Util
{
    public static class FunctionStateUtils
    {
        private const int FASTER_POLLING_INTERVAL = 5000;
        private const int SLOWER_POLLING_INTERVAL = 15000;
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(FunctionStateUtils));

        /// <summary>
        /// Method that polls and refreshes function's state and related fields asynchronously
        /// </summary>
        public static void BeginStatePolling(ViewFunctionController controller, CancellationToken token)
        {
            Task.Run(function: async () => { await FetchAndUpdateFunctionStateAsync(controller, token); },
                cancellationToken: token);
        }

        private static async Task FetchAndUpdateFunctionStateAsync(ViewFunctionController controller,
            CancellationToken token)
        {
            try
            {
                var interval = FASTER_POLLING_INTERVAL;
                do
                {
                    //poll in intervals to get function configuration
                    await Task.Delay(interval, token);

                    var response = await controller.GetFunctionConfigurationAsync(token);

                    //poll slowly if state is active and last update status is successful
                    bool slowerFlag = controller.Model.State.Equals(State.Active) &&
                                      (controller.Model.LastUpdateStatus).Equals(LastUpdateStatus.Successful);

                    interval = slowerFlag ? SLOWER_POLLING_INTERVAL : FASTER_POLLING_INTERVAL;

                    ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread(() =>
                    {
                        controller.Model.State = response.State;
                        controller.Model.StateReasonCode = response.StateReasonCode;
                        controller.Model.StateReason = response.StateReason;
                        controller.Model.LastUpdateStatus = response.LastUpdateStatus;
                        controller.Model.LastUpdateStatusReasonCode = response.LastUpdateStatusReasonCode;
                    });
                } while (!token.IsCancellationRequested);
            }
            catch (TaskCanceledException)
            {
                // Do nothing - we expect refresh to stop with the token cancellation
            }
            catch (Exception e)
            {
                LOGGER.Error("Get function configuration encountered an error", e);
            }
        }
    }
}