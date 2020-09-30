using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Telemetry.Model;
using Amazon.Runtime;
using Amazon.ToolkitTelemetry;
using log4net;

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
            Guid clientId,
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
                ClientID = request.ClientId.ToString(),
                MetricData = request.TelemetryMetrics.AsMetricDatums().ToList(),
            };

            request.ProductEnvironment.ApplyTo(clientRequest);

            await _telemetry.PostMetricsAsync(clientRequest, cancellationToken);
        }

        // TODO : PostFeedback wrapper

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