using System;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Install;
using Amazon.AwsToolkit.CodeWhisperer.Telemetry;
using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

using Moq;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Telemetry
{
    public class LspTelemetryExtensionMethodsTests
    {
        private readonly Mock<ITelemetryLogger> _telemetryLogger = new Mock<ITelemetryLogger>();
        private Result _result;


        [Fact]
        public async Task ExecuteTimeAndRecord_RecordFailedWhenNullObject()
        {
            async Task<LspInstallResult> ExecuteAsync() => null;

            await _telemetryLogger.Object.ExecuteTimeAndRecordAsync(ExecuteAsync, Record);

            Assert.Equal(_result, Result.Failed);
        }

        [Fact]
        public async Task ExecuteTimeAndRecord_RecordFailedWhenNullString()
        {
            async Task<string> ExecuteAsync() => null;

            await _telemetryLogger.Object.ExecuteTimeAndRecordAsync(ExecuteAsync, Record);

            Assert.Equal(_result, Result.Failed);
        }

        [Fact]
        public async Task ExecuteTimeAndRecord_RecordFailedWhenExceptionThrown()
        {
            async Task<string> ExecuteAsync() => throw new Exception("sample-error");

            await Assert.ThrowsAsync<Exception>(async () =>
                await _telemetryLogger.Object.ExecuteTimeAndRecordAsync(ExecuteAsync, Record));

            Assert.Equal(_result, Result.Failed);
        }

        [Fact]
        public async Task ExecuteTimeAndRecord_RecordCancelledWhenOperationCancelledExceptionThrown()
        {
            async Task<string> ExecuteAsync() => throw new OperationCanceledException("sample-error");

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await _telemetryLogger.Object.ExecuteTimeAndRecordAsync(ExecuteAsync, Record));

            Assert.Equal(_result, Result.Cancelled);
        }

        [Fact]
        public async Task ExecuteTimeAndRecord_RecordSuccess()
        {
            async Task<string> ExecuteAsync() => "hello";

            await _telemetryLogger.Object.ExecuteTimeAndRecordAsync(ExecuteAsync, Record);

            Assert.Equal(_result, Result.Succeeded);
        }


        private void Record<T>(ITelemetryLogger arg1, T value, TaskResult taskResult, long duration)
        {
            _result = taskResult.Status.AsTelemetryResult();
        }
    }
}
