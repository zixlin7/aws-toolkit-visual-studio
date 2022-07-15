using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Telemetry.Model;
using Amazon.Runtime;
using Amazon.ToolkitTelemetry;
using Amazon.ToolkitTelemetry.Model;

using log4net;

using Sentiment = Amazon.AwsToolkit.Telemetry.Events.Core.Sentiment;

namespace Amazon.AWSToolkit.Telemetry.Internal
{
    /// <summary>
    /// Telemetry client that wraps the auto-generated client, and performs
    /// required data marshalling.
    /// </summary>
    public class TelemetryClient : ITelemetryClient, IDisposable
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(TelemetryClient));

        private bool _disposed = false;
        private IAmazonToolkitTelemetry _telemetry;
        private readonly ProductEnvironment _productEnvironment;

        public TelemetryClient(AWSCredentials credentials, string serviceUrl, ProductEnvironment productEnvironment)
            : this(new AmazonToolkitTelemetryClient(credentials, new AmazonToolkitTelemetryConfig()
            {
                RegionEndpoint = RegionEndpoint.USEast1,
                ServiceURL = serviceUrl
            }), productEnvironment)
        {
        }

        public TelemetryClient(IAmazonToolkitTelemetry telemetry, ProductEnvironment productEnvironment)
        {
            _telemetry = telemetry;
            _productEnvironment = productEnvironment;
        }

        public async Task PostMetrics(
            ClientId clientId,
            IList<Metrics> telemetryMetrics,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new PostMetricsRequest()
            {
                ClientId = clientId,
                TelemetryMetrics = telemetryMetrics,
                ProductEnvironment = _productEnvironment,
            };

            await PostMetrics(request, cancellationToken);
        }

        public async Task PostMetrics(
            PostMetricsRequest request,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var clientRequest = new Amazon.ToolkitTelemetry.Model.PostMetricsRequest()
            {
                ClientID = request.ClientId,
                MetricData = request.TelemetryMetrics.AsMetricDatums().ToList(),
            };

            request.ProductEnvironment.ApplyTo(clientRequest);

            await _telemetry.PostMetricsAsync(clientRequest, cancellationToken);
        }

        public async Task SendFeedback(Sentiment sentiment, string comment, IDictionary<string, string> metadata)
        {
            var request = new PostFeedbackRequest()
            {
                Sentiment = new ToolkitTelemetry.Sentiment(sentiment.Value),
                Comment = comment
            };
            _productEnvironment.ApplyTo(request);

            metadata.ApplyTo(request);

            await _telemetry.PostFeedbackAsync(request);
        }

        public void Dispose()
        {
            try
            {
                if (_disposed)
                {
                    return;
                }

                if (_telemetry != null)
                {
                    _telemetry.Dispose();
                    _telemetry = null;
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("TelemetryClient Dispose failure", e);
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}
