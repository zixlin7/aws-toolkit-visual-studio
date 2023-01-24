using System;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.EC2.ViewModels;
using Amazon.AWSToolkit.Navigator;

using log4net;

namespace Amazon.AWSToolkit.EC2.Commands
{
    public class AttachElasticIpToInstanceCommand : InstanceCommand
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(AttachElasticIpToInstanceCommand));

        private AttachElasticIPToInstanceModel _model;

        public AttachElasticIpToInstanceCommand(ViewInstancesViewModel viewModel, AwsConnectionSettings awsConnectionSettings,
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

            // instance must not already have an Elastic IP
            return string.IsNullOrWhiteSpace(selectedInstances.Single().ElasticIPAddress);
        }

        protected override async Task<bool> PromptAsync(InstanceCommandArgs args)
        {
            var instance = args.Grid.GetSelectedItems<RunningInstanceWrapper>().Single(i => !i.IsTerminated());

            var elasticIps = (await _viewModel.GetAvailableElasticIpsAsync(instance)).ToList();

            _model = new AttachElasticIPToInstanceModel(instance)
            {
                AvailableAddresses = elasticIps, SelectedAddress = elasticIps.FirstOrDefault(),
            };

            var control = new AttachElasticIPToInstanceControl(_model);
            return _toolkitContext.ToolkitHost.ShowModal(control);
        }

        protected override async Task ExecuteAsync(InstanceCommandArgs args)
        {
            var instance = args.Grid.GetSelectedItems<RunningInstanceWrapper>().Single(i => !i.IsTerminated());
            var domain = ViewInstancesViewModel.GetDomain(instance);

            if (_model.ActionCreateNewAddress)
            {
                await _viewModel.InstanceRepository.AssociateWithNewElasticIpAsync(
                    instance.InstanceId, instance.VpcId, domain);
            }
            else
            {
                await _viewModel.InstanceRepository.AssociateWithElasticIpAsync(instance.InstanceId, instance.VpcId,
                    _model.SelectedAddress.PublicIp, _model.SelectedAddress.AllocationId);
            }

            await _viewModel.ReloadInstancesAsync();
            var selectedInstance = _viewModel.GetInstanceModel(instance.InstanceId);
            if (selectedInstance != null)
            {
                args.Grid.SelectAndScrollIntoView(selectedInstance);
            }
        }

        protected override void HandleExecuteException(Exception ex)
        {
            _logger.Error("Failed to attach Elastic IP to EC2 instance", ex);
            _toolkitContext.ToolkitHost.ShowError($"Error attaching Elastic IP to instance: {ex.Message}");
        }

        protected override void RecordMetric(ActionResults result)
        {
            var data = CreateMetricData<Ec2EditInstanceElasticIp>(result);
            data.Result = result.AsTelemetryResult();
            data.Enabled = true;

            _toolkitContext.TelemetryLogger.RecordEc2EditInstanceElasticIp(data);
        }
    }
}
