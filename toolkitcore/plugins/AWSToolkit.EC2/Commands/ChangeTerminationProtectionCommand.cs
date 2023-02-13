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
    public class ChangeTerminationProtectionCommand : InstanceCommand
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ChangeTerminationProtectionCommand));

        private ChangeTerminationProtectionModel _model;

        public ChangeTerminationProtectionCommand(ViewInstancesViewModel viewModel, AwsConnectionSettings awsConnectionSettings,
            ToolkitContext toolkitContext)
            : base(viewModel, awsConnectionSettings, toolkitContext)
        {
        }

        protected override bool CanExecuteCore(InstanceCommandArgs args)
        {
            return args.Grid.GetSelectedItems<RunningInstanceWrapper>()
                .Count(instance => !instance.IsTerminated()) == 1;
        }

        protected override async Task<bool> PromptAsync(InstanceCommandArgs args)
        {
            var instance = args.Grid.GetSelectedItems<RunningInstanceWrapper>()
                .Single(i => !i.IsTerminated());

            var isEnabled = await _viewModel.InstanceRepository.IsTerminationProtectedAsync(instance.InstanceId);

            _model = new ChangeTerminationProtectionModel(instance.NativeInstance.InstanceId)
            {
                IsProtectionInitiallyEnabled = isEnabled,
                IsProtectionEnabled = isEnabled,
            };

            var control = new ChangeTerminationProtectionControl(_model);
            return _toolkitContext.ToolkitHost.ShowModal(control);
        }

        protected override async Task ExecuteAsync(InstanceCommandArgs args)
        {
            if (_model.IsProtectionEnabled != _model.IsProtectionInitiallyEnabled)
            {
                await _viewModel.InstanceRepository.SetTerminationProtectionAsync(_model.InstanceId,
                    _model.IsProtectionEnabled);
            }
        }

        protected override void HandleExecuteException(Exception ex)
        {
            _logger.Error("Failed to change termination protection for EC2 instance", ex);
            _toolkitContext.ToolkitHost.ShowError($"Error changing termination protection: {ex.Message}");
        }

        protected override void RecordMetric(ActionResults result)
        {
            var data = CreateMetricData<Ec2EditInstanceTerminationProtection>(result);
            data.Result = result.AsTelemetryResult();
            data.Enabled = _model?.IsProtectionEnabled;

            _toolkitContext.TelemetryLogger.RecordEc2EditInstanceTerminationProtection(data);
        }
    }
}
