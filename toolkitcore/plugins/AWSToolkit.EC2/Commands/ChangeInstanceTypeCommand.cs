using System;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Collections;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.EC2.ViewModels;
using Amazon.AWSToolkit.Navigator;

using log4net;

namespace Amazon.AWSToolkit.EC2.Commands
{
    public class ChangeInstanceTypeCommand : InstanceCommand
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ChangeInstanceTypeCommand));

        private ChangeInstanceTypeModel _model;

        public ChangeInstanceTypeCommand(ViewInstancesViewModel viewModel, AwsConnectionSettings awsConnectionSettings,
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

            return EC2Constants.INSTANCE_STATE_STOPPED.Equals(selectedInstances.Single().NativeInstance.State.Name);
        }

        protected override async Task<bool> PromptAsync(InstanceCommandArgs args)
        {
            var instance = args.Grid.GetSelectedItems<RunningInstanceWrapper>()
                .Single(i => !i.IsTerminated());

            var instanceTypes =
                (await _viewModel.InstanceRepository.GetSupportingInstanceTypes(instance.NativeInstance.ImageId))
                .ToList();

            _model = new ChangeInstanceTypeModel(instance.NativeInstance.InstanceId);
            _model.InstanceTypes.AddAll(instanceTypes);
            _model.SelectedInstanceType =
                instanceTypes.FirstOrDefault(t => string.Equals(t.Id, instance.NativeInstance.InstanceType));

            var control = new ChangeInstanceTypeControl(_model);
            return _toolkitContext.ToolkitHost.ShowModal(control);
        }

        protected override async Task ExecuteAsync(InstanceCommandArgs args)
        {
            var instance = args.Grid.GetSelectedItems<RunningInstanceWrapper>()
                .Single(i => !i.IsTerminated());

            var instanceTypeId = _model.SelectedInstanceType.Id;

            await _viewModel.InstanceRepository.UpdateInstanceTypeAsync(instance.InstanceId, instanceTypeId);

            await _viewModel.ReloadInstancesAsync();

            var selectedItem = _viewModel.GetInstanceModel(instance.InstanceId);
            if (selectedItem != null)
            {
                // BUG : when we reload the instances, sometimes the instance type hasn't been updated on the instance yet.
                // HACK : Apply the new value directly to the UI model. Even though this doesn't appear in the grid correctly,
                // HACK : re-entering the change instance type workflow will display the correct type.
                selectedItem.InstanceType = instanceTypeId;
                args.Grid.SelectAndScrollIntoView(selectedItem);
            }
        }

        protected override void HandleExecuteException(Exception ex)
        {
            _logger.Error("Failed to change EC2 instance type", ex);
            _toolkitContext.ToolkitHost.ShowError($"Error changing instance type: {ex.Message}");
        }

        protected override void RecordMetric(ActionResults result)
        {
            var data = CreateMetricData<Ec2EditInstanceType>(result);
            data.Result = result.AsTelemetryResult();

            _toolkitContext.TelemetryLogger.RecordEc2EditInstanceType(data);
        }
    }
}
