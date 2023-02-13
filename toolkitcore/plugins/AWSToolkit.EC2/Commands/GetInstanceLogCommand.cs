using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

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
    public class GetInstanceLogCommand : InstanceCommand
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(GetInstanceLogCommand));

        public GetInstanceLogCommand(ViewInstancesViewModel viewModel, AwsConnectionSettings awsConnectionSettings,
            ToolkitContext toolkitContext)
            : base(viewModel, awsConnectionSettings, toolkitContext)
        {
        }

        protected override bool CanExecuteCore(InstanceCommandArgs args)
        {
            return args.Grid.GetSelectedItems<RunningInstanceWrapper>()
                .Count(instance => !instance.IsTerminated()) == 1;
        }

        protected override async Task ExecuteAsync(InstanceCommandArgs args)
        {
            var instance = args.Grid.GetSelectedItems<RunningInstanceWrapper>()
                .Single(i => !i.IsTerminated());

            var response = await _viewModel.InstanceRepository.GetInstanceLogAsync(instance.InstanceId);

            var model = new GetConsoleOutputModel
            {
                InstanceId = instance.InstanceId,
                Timestamp = response.Timestamp,
                ConsoleOutput = StringUtils.DecodeFrom64(response.Log),
            };

            _toolkitContext.ToolkitHost.ShowModal(new GetConsoleOutputControl(model), MessageBoxButton.OK);
        }

        protected override void HandleExecuteException(Exception ex)
        {
            _logger.Error("Unable to get or show EC2 Instance system log", ex);
            _toolkitContext.ToolkitHost.ShowError($"Error getting console output: {ex.Message}");
        }

        protected override void RecordMetric(ActionResults result)
        {
            var data = CreateMetricData<Ec2ViewInstanceSystemLog>(result);
            data.Result = result.AsTelemetryResult();

            _toolkitContext.TelemetryLogger.RecordEc2ViewInstanceSystemLog(data);
        }
    }
}
