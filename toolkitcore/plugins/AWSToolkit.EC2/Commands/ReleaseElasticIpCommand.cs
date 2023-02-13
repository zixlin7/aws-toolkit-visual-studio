using System;
using System.Threading.Tasks;

using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Commands;

using log4net;

namespace Amazon.AWSToolkit.EC2.Commands
{
    internal class ReleaseElasticIpCommand : PromptAndExecuteHandler<ElasticIpCommandArgs>
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ReleaseElasticIpCommand));

        private readonly ElasticIpCommandState _state;

        public ReleaseElasticIpCommand(ElasticIpCommandState state)
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
            return args.Grid.SelectedItems.Count == 1
                   && !args.GetSelectedAddress().IsAddressInUse;
        }

        public override Task<bool> PromptAsync(ElasticIpCommandArgs args)
        {
            var message = $"Are you sure you want to release Elastic IP with address {args.GetSelectedAddress().PublicIp}?";
            var result = _state.ToolkitContext.ToolkitHost.Confirm("Release Elastic IP", message);
            return Task.FromResult(result);
        }

        public override async Task ExecuteAsync(ElasticIpCommandArgs args)
        {
            await _state.ElasticIpRepository.ReleaseElasticIpAsync(args.GetSelectedAddress());
            await _state.RefreshElasticIpsAsync();
        }

        public override void HandleExecuteException(Exception ex)
        {
            _logger.Error("Error releasing Elastic IP", ex);
            _state.ToolkitContext.ToolkitHost.ShowError("Release Elastic IP Error", "Error releasing address: " + ex.Message);
        }

        public override void RecordMetric(ToolkitCommandResult result)
        {
            var data = result.CreateMetricData<Ec2DeleteElasticIp>(_state.AwsConnectionSettings,
                _state.ToolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();

            _state.ToolkitContext.TelemetryLogger.RecordEc2DeleteElasticIp(data);
        }
    }
}
