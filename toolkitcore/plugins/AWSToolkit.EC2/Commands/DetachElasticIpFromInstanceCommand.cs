using System;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.ViewModels;
using Amazon.AWSToolkit.Navigator;

using log4net;

namespace Amazon.AWSToolkit.EC2.Commands
{
    public class DetachElasticIpFromInstanceCommand : InstanceCommand
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(DetachElasticIpFromInstanceCommand));

        public DetachElasticIpFromInstanceCommand(ViewInstancesViewModel viewModel, AwsConnectionSettings awsConnectionSettings,
            ToolkitContext toolkitContext)
            : base(viewModel, awsConnectionSettings, toolkitContext)
        {
        }

        protected override bool CanExecuteCore(InstanceCommandArgs args)
        {
            var selectedInstances = args.Grid.GetSelectedItems<RunningInstanceWrapper>()
                .Where(instance => !instance.IsTerminated())
                .ToArray();
            
            if (selectedInstances.Length != 1) { return false; }

            // instance must have an Elastic IP
            return !string.IsNullOrWhiteSpace(selectedInstances.Single().ElasticIPAddress);
        }

        protected override bool Prompt(InstanceCommandArgs args)
        {
            var instance = args.Grid.GetSelectedItems<RunningInstanceWrapper>().Single(i => !i.IsTerminated());

            var message = $"Are you sure you want to disassociate the Elastic IP address {instance.ElasticIPAddress} from instance {instance.InstanceId}?";
            return _toolkitContext.ToolkitHost.Confirm("Disassociate Address", message);
        }

        protected override async Task ExecuteAsync(InstanceCommandArgs args)
        {
            var instance = args.Grid.GetSelectedItems<RunningInstanceWrapper>().Single(i => !i.IsTerminated());

            await _viewModel.InstanceRepository.DisassociateElasticIpAsync(instance);

            await _viewModel.ReloadInstancesAsync();
            var selectedInstance = _viewModel.GetInstanceModel(instance.InstanceId);
            if (selectedInstance != null)
            {
                args.Grid.SelectAndScrollIntoView(selectedInstance);
            }
        }

        protected override void HandleExecuteException(Exception ex)
        {
            _logger.Error("Failed to detach Elastic IP from EC2 instance", ex);
            _toolkitContext.ToolkitHost.ShowError($"Error detaching Elastic IP address from instance: {ex.Message}");
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
