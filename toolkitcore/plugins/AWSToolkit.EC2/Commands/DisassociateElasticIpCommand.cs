using System;
using System.Linq;
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
    public class DisassociateElasticIpCommand : SelectedElasticIpCommand
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(DisassociateElasticIpCommand));

        public DisassociateElasticIpCommand(ViewElasticIPsModel viewModel, IElasticIpRepository elasticIp,
            AwsConnectionSettings awsConnectionSettings, ToolkitContext toolkitContext)
            : base(viewModel, elasticIp, awsConnectionSettings, toolkitContext)
        {
        }

        protected override bool CanExecuteCore(SelectedElasticIpCommandArgs args)
        {
            return args.SelectedAddress.IsAddressInUse &&
                   !string.IsNullOrEmpty(args.SelectedAddress.InstanceId);
        }

        protected override async Task<bool> PromptAsync(SelectedElasticIpCommandArgs args)
        {
            var message =
                $"Are you sure you want to disassociate the Elastic IP with address {args.SelectedAddress.PublicIp} from Instance {args.SelectedAddress.InstanceId}?";
            return _toolkitContext.ToolkitHost.Confirm("Disassociate Elastic IP", message);
        }

        protected override async Task ExecuteAsync(SelectedElasticIpCommandArgs args)
        {
            await _elasticIp.DisassociateFromInstanceAsync(args.SelectedAddress);
            await RefreshElasticIpsAsync();

            var addressToSelect = _viewModel.Addresses.FirstOrDefault(x => x.PublicIp == args.SelectedAddress.PublicIp);
            if (addressToSelect != null)
            {
                args.Grid.SelectAndScrollIntoView(addressToSelect);
            }
        }

        protected override void HandleExecuteException(Exception ex)
        {
            _logger.Error("Error disassociating Elastic IP", ex);
            _toolkitContext.ToolkitHost.ShowError("Disassociate Elastic IP Error",
                $"Error disassociating address: {ex.Message}");
        }

        protected override void RecordMetric(ActionResults result)
        {
            var data = CreateMetricData<Ec2EditInstanceElasticIp>(result);
            data.Result = result.AsTelemetryResult();
            data.Enabled = false;

            _toolkitContext.TelemetryLogger.RecordEc2EditInstanceElasticIp(data);
        }
    }
}
