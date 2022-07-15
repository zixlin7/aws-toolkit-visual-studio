using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Telemetry;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Telemetry.Internal;
using Amazon.AWSToolkit.Telemetry.Model;
using Amazon.ToolkitTelemetry;
using Amazon.ToolkitTelemetry.Model;

using Moq;
using Newtonsoft.Json;
using Xunit;

using PostMetricsRequest = Amazon.AWSToolkit.Telemetry.Internal.PostMetricsRequest;
using SdkPostMetricsRequest = Amazon.ToolkitTelemetry.Model.PostMetricsRequest;
using Sentiment = Amazon.ToolkitTelemetry.Sentiment;

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
            var telemetryMetric = new Metrics()
            {
                CreatedOn = DateTime.Now,
                Data = Enumerable
                    .Range(1, Randomizer.Next(1, 3))
                    .Select(x => TestHelper.CreateSampleMetricDatum(Randomizer.Next(1, 5))).ToList()
            };

            _sampleClientRequest = new PostMetricsRequest()
            {
                ClientId = ClientId.AutomatedTestClientId,
                TelemetryMetrics = new List<Metrics>() {telemetryMetric},
                ProductEnvironment = ProductEnvironment
            };

            _sampleSdkRequest = new SdkPostMetricsRequest()
            {
                ClientID = _sampleClientRequest.ClientId.ToString(),
                MetricData = telemetryMetric.AsMetricDatums().ToList(),
            };

            ProductEnvironment.ApplyTo(_sampleSdkRequest);
        }

        [Fact]
        public async Task WithClientIdAndEvents()
        {
            await TelemetryClient.PostMetrics(_sampleClientRequest.ClientId, _sampleClientRequest.TelemetryMetrics);

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


    public class TelemetryClientPostFeedbackTests : TelemetryClientTestsBase
    {
        private PostFeedbackRequest _sampleClientRequest;
        private Dictionary<string, string> _sampleMetadata = new Dictionary<string, string> { {"abc", "def" } };

        public TelemetryClientPostFeedbackTests() : base()
        {
            _sampleClientRequest = new PostFeedbackRequest()
            {
                Sentiment = Sentiment.Positive,
                Comment = "good"
            };

            ProductEnvironment.ApplyTo(_sampleClientRequest);
        }


        public static IEnumerable<object[]> NoMetadataFeedback = new List<object[]>
        {
            new object[] { null },
            new object[] {new Dictionary<string, string>() }
        };


        [Theory]
        [MemberData(nameof(NoMetadataFeedback))]
        public async Task SendFeedback(IDictionary<string, string> metadata)
        {
            await TelemetryClient.SendFeedback(AwsToolkit.Telemetry.Events.Core.Sentiment.Positive, "good", metadata);

            TelemetrySdk.Verify(mock => mock.PostFeedbackAsync(
                It.Is<PostFeedbackRequest>(request => AreEqual(_sampleClientRequest, request)),
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        [Fact]
        public async Task SendFeedback_WithMetadata()
        {
            _sampleMetadata.ApplyTo(_sampleClientRequest);
            await TelemetryClient.SendFeedback(AwsToolkit.Telemetry.Events.Core.Sentiment.Positive, "good", _sampleMetadata);

            TelemetrySdk.Verify(mock => mock.PostFeedbackAsync(
                It.Is<PostFeedbackRequest>(request => AreEqual(_sampleClientRequest, request)),
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        private bool AreEqual(PostFeedbackRequest expectedRequest, PostFeedbackRequest actualRequest)
        {
            var expectedJson = JsonConvert.SerializeObject(expectedRequest);
            var actualJson = JsonConvert.SerializeObject(actualRequest);

            return expectedJson == actualJson;
        }
    }
}
