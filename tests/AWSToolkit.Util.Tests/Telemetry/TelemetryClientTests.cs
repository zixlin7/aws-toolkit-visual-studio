using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Telemetry;
using Amazon.AWSToolkit.Telemetry.Internal;
using Amazon.AWSToolkit.Telemetry.Model;
using Amazon.ToolkitTelemetry;
using Moq;
using Newtonsoft.Json;
using Xunit;
using SdkPostMetricsRequest = Amazon.ToolkitTelemetry.Model.PostMetricsRequest;

namespace Amazon.AWSToolkit.Util.Tests.Telemetry
{
    public class TelemetryClientTestsBase
    {
        protected static readonly Random Randomizer = new Random();
        protected Mock<IAmazonToolkitTelemetry> TelemetrySdk = new Mock<IAmazonToolkitTelemetry>();
        protected TelemetryClient TelemetryClient;
        protected readonly ProductEnvironment ProductEnvironment = new ProductEnvironment()
        {
            AwsProduct = AWSProduct.AWSToolkitForVisualStudio,
            AwsProductVersion = "testVersion",
            OperatingSystem = "testOs",
            OperatingSystemVersion = "1.2.3",
            ParentProduct = "testParent",
            ParentProductVersion = "4.5.6",
        };

        public TelemetryClientTestsBase()
        {
            TelemetryClient = new TelemetryClient(TelemetrySdk.Object, ProductEnvironment);
        }
    }

    public class TelemetryClientDisposeTests : TelemetryClientTestsBase
    {
        [Fact]
        public void DisposesClient()
        {
            TelemetryClient.Dispose();
            TelemetrySdk.Verify(mock => mock.Dispose(), Times.Once);
        }
    }

    public class TelemetryClientPostMetricsTests : TelemetryClientTestsBase
    {
        private readonly SdkPostMetricsRequest _sampleSdkRequest;
        private readonly PostMetricsRequest _sampleClientRequest;

        public TelemetryClientPostMetricsTests() : base()
        {
            var telemetryEvent = new TelemetryEvent()
            {
                CreatedOn = DateTime.Now,
                Data = Enumerable
                    .Range(1, Randomizer.Next(1, 3))
                    .Select(x => TestHelper.CreateSampleMetricDatum(Randomizer.Next(1, 5))).ToList()
            };

            _sampleClientRequest = new PostMetricsRequest()
            {
                ClientId = Guid.NewGuid(),
                TelemetryEvents = new List<TelemetryEvent>() {telemetryEvent},
                ProductEnvironment = ProductEnvironment
            };

            _sampleSdkRequest = new SdkPostMetricsRequest()
            {
                ClientID = _sampleClientRequest.ClientId.ToString(),
                MetricData = telemetryEvent.AsMetricDatums().ToList(),
            };

            ProductEnvironment.ApplyTo(_sampleSdkRequest);
        }

        [Fact]
        public async Task WithClientIdAndEvents()
        {
            await TelemetryClient.PostMetrics(_sampleClientRequest.ClientId, _sampleClientRequest.TelemetryEvents);

            TelemetrySdk.Verify(mock => mock.PostMetricsAsync(
                It.Is<SdkPostMetricsRequest>(request => AreEqual(_sampleSdkRequest, request)),
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        [Fact]
        public async Task WithRequest()
        {
            await TelemetryClient.PostMetrics(_sampleClientRequest);

            TelemetrySdk.Verify(mock => mock.PostMetricsAsync(
                It.Is<SdkPostMetricsRequest>(request => AreEqual(_sampleSdkRequest, request)),
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        private bool AreEqual(SdkPostMetricsRequest expectedRequest, SdkPostMetricsRequest actualRequest)
        {
            var expectedJson = JsonConvert.SerializeObject(expectedRequest);
            var actualJson = JsonConvert.SerializeObject(actualRequest);

            return expectedJson == actualJson;
        }
    }
}