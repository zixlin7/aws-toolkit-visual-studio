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
    public class ChangeShutdownBehaviorCommand : InstanceCommand
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ChangeShutdownBehaviorCommand));

        private ChangeShutdownBehaviorModel _model;

        public ChangeShutdownBehaviorCommand(ViewInstancesViewModel viewModel, AwsConnectionSettings awsConnectionSettings,
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

            _model = new ChangeShutdownBehaviorModel(instance.NativeInstance.InstanceId)
            {
                SelectedOption = await _viewModel.InstanceRepository.GetShutdownBehaviorAsync(instance.InstanceId)
            };

            var control = new ChangeShutdownBehaviorControl(_model);
            return _toolkitContext.ToolkitHost.ShowModal(control);
        }

        protected override async Task ExecuteAsync(InstanceCommandArgs args)
        {
            if (!string.Equals(_model.SelectedOption, _model.InitialOption))
            {
                await _viewModel.InstanceRepository.SetShutdownBehaviorAsync(_model.InstanceId, _model.SelectedOption);
            }
        }

        protected override void HandleExecuteException(Exception ex)
        {
            _logger.Error("Failed to change shutdown behavior for EC2 instance", ex);
            _toolkitContext.ToolkitHost.ShowError($"Error changing shutdown behavior: {ex.Message}");
        }

        protected override void RecordMetric(ActionResults result)
        {
            var data = CreateMetricData<Ec2EditInstanceShutdownBehavior>(result);
            data.Result = result.AsTelemetryResult();

            _toolkitContext.TelemetryLogger.RecordEc2EditInstanceShutdownBehavior(data);
        }
    }
}
