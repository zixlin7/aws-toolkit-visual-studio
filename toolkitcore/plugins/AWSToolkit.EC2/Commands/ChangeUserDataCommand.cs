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
using Amazon.AWSToolkit.Util;

using log4net;

namespace Amazon.AWSToolkit.EC2.Commands
{
    public class ChangeUserDataCommand : InstanceCommand
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ChangeUserDataCommand));

        private ChangeUserDataModel _model;

        public ChangeUserDataCommand(ViewInstancesViewModel viewModel, AwsConnectionSettings awsConnectionSettings,
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

            var userData = await _viewModel.InstanceRepository.GetUserDataAsync(instance.InstanceId);
            var decodedUserData = StringUtils.DecodeFrom64(userData);

            _model = new ChangeUserDataModel(instance)
            {
                InitialUserData = decodedUserData, UserData = decodedUserData
            };

            var control = new ChangeUserDataControl(_model);
            return _toolkitContext.ToolkitHost.ShowModal(control);
        }

        protected override async Task ExecuteAsync(InstanceCommandArgs args)
        {
            if (IsUserDataChanged())
            {
                await _viewModel.InstanceRepository.SetUserDataAsync(_model.InstanceId, _model.UserData);
            }
        }

        private bool IsUserDataChanged()
        {
            return _model != null && _model.InitialUserData != _model.UserData;
        }

        protected override void HandleExecuteException(Exception ex)
        {
            _logger.Error("Failed to change EC2 Instance user data", ex);
            _toolkitContext.ToolkitHost.ShowError($"Error changing user data: {ex.Message}");
        }

        protected override void RecordMetric(ActionResults result)
        {
            RecordView(result);

            if (!result.Cancelled && IsUserDataChanged())
            {
                RecordEdit(result);
            }
        }

        private void RecordView(ActionResults result)
        {
            var data = CreateMetricData<Ec2ViewInstanceUserData>(result);
            data.Result = result.AsTelemetryResult();

            _toolkitContext.TelemetryLogger.RecordEc2ViewInstanceUserData(data);
        }

        private void RecordEdit(ActionResults result)
        {
            var data = CreateMetricData<Ec2EditInstanceUserData>(result);
            data.Result = result.AsTelemetryResult();

            _toolkitContext.TelemetryLogger.RecordEc2EditInstanceUserData(data);
        }
    }
}
