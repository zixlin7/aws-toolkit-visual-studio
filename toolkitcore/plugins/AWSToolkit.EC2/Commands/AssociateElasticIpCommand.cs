using System;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;

using log4net;

namespace Amazon.AWSToolkit.EC2.Commands
{
    internal class AssociateElasticIpCommand : PromptAndExecuteHandler<ElasticIpCommandArgs>
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(AssociateElasticIpCommand));

        private readonly ElasticIpCommandState _state;

        private string _instanceId;

        public AssociateElasticIpCommand(ElasticIpCommandState state)
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

        public override async Task<bool> PromptAsync(ElasticIpCommandArgs args)
        {
            var address = args.GetSelectedAddress();
            var model = new AssociateAddressModel(address);
            model.AvailableInstances =
                (await _state.ElasticIpRepository.GetUnassociatedInstancesAsync(address.Domain)).ToList();

            if (model.AvailableInstances.Count == 0)
            {
                throw new Ec2Exception(
                    "There are either no running EC2 instances that can be associated with an Elastic IP, or all running instances are already associated with one.",
                    Ec2Exception.Ec2ErrorCode.NoInstances);
            }

            model.Instance = model.AvailableInstances.FirstOrDefault();

            var control = new AssociateAddressControl(model);
            var result = _state.ToolkitContext.ToolkitHost.ShowModal(control);

            if (result)
            {
                _instanceId = model.Instance.InstanceId;
            }

            return result;
        }

        public override async Task ExecuteAsync(ElasticIpCommandArgs args)
        {
            var address = args.GetSelectedAddress();
            await _state.ElasticIpRepository.AssociateWithInstance(address, _instanceId);
            await _state.RefreshElasticIpsAsync();

            var addressToSelect =
                _state.ViewElasticIPsModel.Addresses.FirstOrDefault(x => x.PublicIp == address.PublicIp);
            if (addressToSelect != null)
            {
                args.Grid.SelectAndScrollIntoView(addressToSelect);
            }
        }

        public override void HandleExecuteException(Exception ex)
        {
            _logger.Error("Error associating Elastic IP", ex);
            _state.ToolkitContext.ToolkitHost.ShowError("Associate Elastic IP Error",
                $"Error associating address: {ex.Message}");
        }

        public override void RecordMetric(ToolkitCommandResult result)
        {
            var data = result.CreateMetricData<Ec2EditInstanceElasticIp>(_state.AwsConnectionSettings,
                _state.ToolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();
            data.Enabled = true;

            _state.ToolkitContext.TelemetryLogger.RecordEc2EditInstanceElasticIp(data);
        }
    }
}
