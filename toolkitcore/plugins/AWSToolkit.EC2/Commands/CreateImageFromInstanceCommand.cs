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
    public class CreateImageFromInstanceCommand : InstanceCommand
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(CreateImageFromInstanceCommand));

        private CreateImageModel _createImageModel;

        public CreateImageFromInstanceCommand(ViewInstancesViewModel viewModel, AwsConnectionSettings awsConnectionSettings,
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

            return EC2Constants.ROOT_DEVICE_TYPE_EBS.Equals(selectedInstances.Single().RootDeviceType);
        }

        protected override bool Prompt(InstanceCommandArgs args)
        {
            var instance = args.Grid.GetSelectedItems<RunningInstanceWrapper>().Single(i => !i.IsTerminated());

            _createImageModel = new CreateImageModel(instance.NativeInstance.InstanceId);
            var control = new CreateImageControl(_createImageModel);
            return _toolkitContext.ToolkitHost.ShowModal(control);
        }

        protected override async Task ExecuteAsync(InstanceCommandArgs args)
        {
            var imageId = await _viewModel.InstanceRepository.CreateImageFromInstanceAsync(
                _createImageModel.InstanceId,
                _createImageModel.Name,
                _createImageModel.Description);

            _toolkitContext.ToolkitHost.ShowMessage("Image Created", $"Image was created with id: {imageId}");
        }

        protected override void HandleExecuteException(Exception ex)
        {
            _logger.Error("Failed to create AMI from EC2 instance", ex);
            _toolkitContext.ToolkitHost.ShowError($"Failed to create AMI from EC2 instance: {ex.Message}");
        }

        protected override void RecordMetric(ActionResults result)
        {
            var data = CreateMetricData<Ec2CreateAmi>(result);
            data.Result = result.AsTelemetryResult();

            _toolkitContext.TelemetryLogger.RecordEc2CreateAmi(data);
        }
    }
}
