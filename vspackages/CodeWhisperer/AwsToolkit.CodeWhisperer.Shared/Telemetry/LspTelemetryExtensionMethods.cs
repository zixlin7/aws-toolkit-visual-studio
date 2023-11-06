using System;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Install;
using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.AWSToolkit.Telemetry;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Util;

using TaskStatus = Amazon.AWSToolkit.CommonUI.Notifications.TaskStatus;

namespace Amazon.AwsToolkit.CodeWhisperer.Telemetry
{
    public static class LspTelemetryExtensionMethods
    {
        /// <summary>
        /// Extension method to record time and telemetry event associated with an action that returns a result
        /// </summary>
        /// <typeparam name="T">type of result returned on execution of the action</typeparam>
        /// <param name="telemetryLogger">telemetry logger for recording the metric</param>
        /// <param name="work">action to be performed</param>
        /// <param name="recordMetric">metric to be recorded</param>
        /// <returns>result of the action</returns>
        internal static async Task<T> ExecuteTimeAndRecordAsync<T>(this ITelemetryLogger telemetryLogger, Func<Task<T>> work,
            Action<ITelemetryLogger, T, TaskResult, long> recordMetric)
        {
            var taskResult = new TaskResult();
            T result = default;

            async Task ExecuteAsync()
            {
                try
                {
                    result = await work();
                    taskResult.Status =  IsNull(result) ? TaskStatus.Fail : TaskStatus.Success;
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException)
                    {
                        taskResult.Status = TaskStatus.Cancel;
                    }

                    taskResult.Exception = ex;
                    throw;
                }
            }

            void Record(ITelemetryLogger telemetryLog, long milliseconds) =>
                recordMetric(telemetryLog, result, taskResult, milliseconds);


            await telemetryLogger.TimeAndRecordAsync(ExecuteAsync, Record);
            return result;
        }

        /// <summary>
        /// Extension method to record time and telemetry event associated with an action
        /// </summary>
        /// <param name="telemetryLogger">telemetry logger for recording the metric</param>
        /// <param name="work">action to be performed</param>
        /// <param name="recordMetric">metric to be recorded</param>
        internal static async Task InvokeTimeAndRecordAsync(this ITelemetryLogger telemetryLogger, Func<Task> work,
            Action<ITelemetryLogger, TaskResult, long> recordMetric)
        {
            var taskResult = new TaskResult();

            async Task InvokeAsync()
            {
                try
                {
                    await work();
                    taskResult.Status =  TaskStatus.Success;
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException)
                    {
                        taskResult.Status = TaskStatus.Cancel;
                    }

                    taskResult.Exception = ex;
                    throw;
                }
            }

            void Record(ITelemetryLogger telemetryLog, long milliseconds) =>
                recordMetric(telemetryLog, taskResult, milliseconds);


            await telemetryLogger.TimeAndRecordAsync(InvokeAsync, Record);
        }

        internal static void RecordModifySetting(this ITelemetryLogger telemetryLogger, TaskResult result, string settingId, string state)
        {
            var data = result.CreateMetricData<AwsModifySetting>();
            data.SettingId = settingId;
            data.SettingState = state;
            telemetryLogger.RecordAwsModifySetting(data);
        }

        internal static void RecordSetupAll(this ITelemetryLogger telemetryLogger, TaskResult result, RecordLspInstallerArgs args)
        {
            telemetryLogger.RecordLanguageServerSetup(result, args, LanguageServerSetupStage.All);
        }

        internal static void RecordSetupInitialize(this ITelemetryLogger telemetryLogger, TaskResult result, RecordLspInstallerArgs args)
        {
            telemetryLogger.RecordLanguageServerSetup(result, args, LanguageServerSetupStage.Initialize);
        }

        internal static void RecordSetupGetManifest(this ITelemetryLogger telemetryLogger, TaskResult result, RecordLspInstallerArgs args)
        {
            telemetryLogger.RecordLanguageServerSetup(result, args, LanguageServerSetupStage.GetManifest);
        }

        internal static void RecordSetupGetLsp(this ITelemetryLogger telemetryLogger, TaskResult result, RecordLspInstallerArgs args)
        {
            telemetryLogger.RecordLanguageServerSetup(result, args, LanguageServerSetupStage.GetServer);
        }

        internal static void RecordLanguageServerSetup(this ITelemetryLogger telemetryLogger, TaskResult result, RecordLspInstallerArgs args, LanguageServerSetupStage stage)
        {
            var data = CreateLanguageServerSetupData(result, args, stage);
            telemetryLogger.RecordLanguageServerSetup(data);
        }

        private static LanguageServerSetup CreateLanguageServerSetupData(TaskResult result, RecordLspInstallerArgs args, LanguageServerSetupStage stage)
        {
            var data = result.CreateMetricData<LanguageServerSetup>();
            data.Id = args.Id;
            data.Result = GetResult(result);
            data.Version = args.LanguageServerVersion;
            data.LanguageServerLocation = args.Location;
            data.LanguageServerSetupStage = stage;
            data.ManifestSchemaVersion = args.ManifestSchemaVersion;
            data.Duration = args.Duration;
            data.Value = args.Duration;
            return data;
        }


        private static Result GetResult(TaskResult result)
        {
            return result == null ? Result.Failed : result.Status.AsTelemetryResult();
        }


        private static bool IsNull(object obj)
        {
            if (obj is string s)
            {
                return string.IsNullOrWhiteSpace(s);
            }

            return obj == null;
        }

        /// <summary>
        /// Utility method to create a metric object and pre-populate it with standardized fields.
        /// </summary>
        /// <typeparam name="T">Metric object to instantiate</typeparam>
        /// <param name="result">Operation result, used to populate some of the metric fields</param>
        private static T CreateMetricData<T>(this TaskResult result) where T : BaseTelemetryEvent, new()
        {
            var metricData = new T();
            metricData.AwsAccount = MetadataValue.NotApplicable;
            metricData.AwsRegion = MetadataValue.NotApplicable;

            // add error metadata if failed result is encountered
            if (result != null && result.Status.AsTelemetryResult().Equals(Result.Failed))
            {
                metricData.AddErrorMetadata(result.Exception);
            }

            return metricData;
        }
    }
}
