using System;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.EC2.View.DataGrid;

using log4net;

namespace Amazon.AWSToolkit.EC2.Commands
{
    internal class AllocateElasticIpCommand : PromptAndExecuteHandler<ElasticIpCommandArgs>
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(AllocateElasticIpCommand));

        private readonly ElasticIpCommandState _state;

        private string _ipDomain;

        public AllocateElasticIpCommand(ElasticIpCommandState state)
        {
            Arg.NotNull(state, nameof(state));

            _state = state;
        }

        public override ElasticIpCommandArgs AsHandlerArgs(object parameter)
        {
            var args = new ElasticIpCommandArgs { Grid = parameter as ICustomizeColumnGrid };

            Arg.NotNull(args.Grid, nameof(parameter));

            return args;
        }

        public override Task<bool> PromptAsync(ElasticIpCommandArgs args)
        {
            var control = new AllocateAddressControl();
            if (!_state.ToolkitContext.ToolkitHost.ShowModal(control))
            {
                return Task.FromResult(false);
            }

            _ipDomain = control.Domain;
            return Task.FromResult(true);
        }

        public override async Task ExecuteAsync(ElasticIpCommandArgs args)
        {
            var publicId =
                await _state.ElasticIpRepository.AllocateElasticIpAsync(_ipDomain);

            await _state.RefreshElasticIpsAsync();

            var addressToSelect = _state.ViewElasticIPsModel.Addresses.FirstOrDefault(x => x.PublicIp == publicId);
            if (addressToSelect != null)
            {
                args.Grid.SelectAndScrollIntoView(addressToSelect);
            }
        }

        public override void HandleExecuteException(Exception ex)
        {
            _logger.Error("Error allocating Elastic IP", ex);
            _state.ToolkitContext.ToolkitHost.ShowError("Allocate Elastic IP Error", "Error allocating Elastic IP: " + ex.Message);
        }

        public override void RecordMetric(ToolkitCommandResult result)
        {
            var data = result.CreateMetricData<Ec2CreateElasticIp>(_state.AwsConnectionSettings, _state.ToolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();

            _state.ToolkitContext.TelemetryLogger.RecordEc2CreateElasticIp(data);
        }
    }
}
