using System;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Commands;

using log4net;

namespace Amazon.AWSToolkit.EC2.Commands
{
    internal class DisassociateElasticIpCommand : PromptAndExecuteHandler<ElasticIpCommandArgs>
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(DisassociateElasticIpCommand));

        private readonly ElasticIpCommandState _state;

        public DisassociateElasticIpCommand(ElasticIpCommandState state)
        {
            Arg.NotNull(state, nameof(state));

            _state = state;
        }

        public override ElasticIpCommandArgs AsHandlerArgs(object parameter)
        {
            return ElasticIpCommandArgs.FromParameter(parameter);
        }

        public override bool CanExecute(ElasticIpCommandArgs args)
        {
            if (args.Grid.SelectedItems.Count != 1) { return false; }

            var selectedAddress = args.GetSelectedAddress();

            return args.Grid.SelectedItems.Count == 1 &&
                   selectedAddress.IsAddressInUse &&
                   !string.IsNullOrEmpty(selectedAddress.InstanceId);
        }

        public override Task<bool> PromptAsync(ElasticIpCommandArgs args)
        {
            var selectedAddress = args.GetSelectedAddress();
            var message =
                $"Are you sure you want to disassociate the Elastic IP with address {selectedAddress.PublicIp} from Instance {selectedAddress.InstanceId}?";
            var result = _state.ToolkitContext.ToolkitHost.Confirm("Disassociate Elastic IP", message);
            return Task.FromResult(result);
        }

        public override async Task ExecuteAsync(ElasticIpCommandArgs args)
        {
            var selectedAddress = args.GetSelectedAddress();
            await _state.ElasticIpRepository.DisassociateFromInstanceAsync(selectedAddress);
            await _state.RefreshElasticIpsAsync();

            var addressToSelect =
                _state.ViewElasticIPsModel.Addresses.FirstOrDefault(x => x.PublicIp == selectedAddress.PublicIp);
            if (addressToSelect != null)
            {
                args.Grid.SelectAndScrollIntoView(addressToSelect);
            }
        }

        public override void HandleExecuteException(Exception ex)
        {
            _logger.Error("Error disassociating Elastic IP", ex);
            _state.ToolkitContext.ToolkitHost.ShowError("Disassociate Elastic IP Error",
                $"Error disassociating address: {ex.Message}");
        }

        public override void RecordMetric(ToolkitCommandResult result)
        {
            var data = result.CreateMetricData<Ec2EditInstanceElasticIp>(_state.AwsConnectionSettings,
                _state.ToolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();
            data.Enabled = false;

            _state.ToolkitContext.TelemetryLogger.RecordEc2EditInstanceElasticIp(data);
        }
    }
}
