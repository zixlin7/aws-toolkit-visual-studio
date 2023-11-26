using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Install;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Telemetry;
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
        private readonly MetricEvent _sampleMetricEvent = new MetricEvent()
        {
            Name = "sample-metric",
            Data = new Dictionary<string, object>() { { "duration", 123 }, { "url", "http://abc.com" } }
        };

        private readonly MetricDatum _metricDatum = new MetricDatum();

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

        [Fact]
        public void TransformAndRecordEvent_WithoutValue()
        {
            _telemetryLogger.Object.TransformAndRecordEvent(_metricDatum, _sampleMetricEvent);

            Assert.Equal(_sampleMetricEvent.Data.Keys, _metricDatum.Metadata.Keys);
            Assert.Equal(_sampleMetricEvent.Name, _metricDatum.MetricName);
            _telemetryLogger.Verify(mock => mock.Record(It.IsAny<Metrics>()), Times.Once);
        }

        [Fact]
        public void TransformAndRecordEvent_WithValue()
        {
            _sampleMetricEvent.Data.Add("value", 13.0);

            _telemetryLogger.Object.TransformAndRecordEvent(_metricDatum, _sampleMetricEvent);

            Assert.Equal(13.0, _metricDatum.Value);
            Assert.Equal(2, _metricDatum.Metadata.Count);
            Assert.Equal(_sampleMetricEvent.Name, _metricDatum.MetricName);
            _telemetryLogger.Verify(mock => mock.Record(It.IsAny<Metrics>()), Times.Once);
        }

        [Fact]
        public void TransformAndRecordEvent_IgnoresDataWhenOverlappingResult()
        {
            _sampleMetricEvent.Data.Add("result", ResultType.Cancelled);
            _sampleMetricEvent.Result = ResultType.Succeeded;

            _telemetryLogger.Object.TransformAndRecordEvent(_metricDatum, _sampleMetricEvent);

            Assert.Equal(3, _metricDatum.Metadata.Count);
            Assert.Equal(_sampleMetricEvent.Name, _metricDatum.MetricName);
            Assert.Equal(_sampleMetricEvent.Result.ToString(), _metricDatum.Metadata["result"]);
            _telemetryLogger.Verify(mock => mock.Record(It.IsAny<Metrics>()), Times.Once);
        }

        [Fact]
        public void TransformAndRecordEvent_IgnoresDataWhenOverlappingErrorData()
        {
            _sampleMetricEvent.Data.Add("reason", "ignore-reason");
            _sampleMetricEvent.ErrorData = new ErrorData(){Reason = "sample-reason"};

            _telemetryLogger.Object.TransformAndRecordEvent(_metricDatum, _sampleMetricEvent);

            Assert.Equal(3, _metricDatum.Metadata.Count);
            Assert.Equal(_sampleMetricEvent.Name, _metricDatum.MetricName);
            Assert.Equal(_sampleMetricEvent.ErrorData.Reason, _metricDatum.Metadata["reason"]);
            Assert.False(_metricDatum.Metadata.ContainsKey("errorCode"));
            Assert.False(_metricDatum.Metadata.ContainsKey("httpStatusCode"));
            _telemetryLogger.Verify(mock => mock.Record(It.IsAny<Metrics>()), Times.Once);
        }



        private void Record<T>(ITelemetryLogger arg1, T value, TaskResult taskResult, long duration)
        {
            _result = taskResult.Status.AsTelemetryResult();
        }
    }
}
