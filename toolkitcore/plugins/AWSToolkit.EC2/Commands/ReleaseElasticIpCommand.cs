using System;
using System.Threading.Tasks;

using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.Repositories;
using Amazon.AWSToolkit.Navigator;

using log4net;

namespace Amazon.AWSToolkit.EC2.Commands
{
    public class ReleaseElasticIpCommand : SelectedElasticIpCommand
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ReleaseElasticIpCommand));

        public ReleaseElasticIpCommand(ViewElasticIPsModel viewModel, IElasticIpRepository elasticIp,
            AwsConnectionSettings awsConnectionSettings, ToolkitContext toolkitContext)
            : base(viewModel, elasticIp, awsConnectionSettings, toolkitContext)
        {
        }

        protected override bool Prompt(AddressWrapper address)
        {
            var message = $"Are you sure you want to release Elastic IP with address {address.PublicIp}?";
            return _toolkitContext.ToolkitHost.Confirm("Release Elastic IP", message);
        }

        protected override async Task ExecuteAsync(AddressWrapper address)
        {
            await _elasticIp.ReleaseElasticIpAsync(address, _awsConnectionSettings);
            await RefreshElasticIpsAsync();
        }

        protected override void HandleExecuteException(Exception ex)
        {
            _logger.Error("Error releasing Elastic IP", ex);
            _toolkitContext.ToolkitHost.ShowError("Release Elastic IP Error", "Error releasing address: " + ex.Message);
        }

        protected override void RecordMetric(ActionResults result)
        {
            var data = CreateMetricData<Ec2DeleteElasticIp>(result);
            data.Result = result.AsTelemetryResult();

            _toolkitContext.TelemetryLogger.RecordEc2DeleteElasticIp(data);
        }
    }
}
