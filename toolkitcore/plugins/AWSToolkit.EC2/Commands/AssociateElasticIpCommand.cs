using System;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.Repositories;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.Navigator;

using log4net;

namespace Amazon.AWSToolkit.EC2.Commands
{
    public class AssociateElasticIpCommand : SelectedElasticIpCommand
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(AssociateElasticIpCommand));

        private string _instanceId;

        public AssociateElasticIpCommand(ViewElasticIPsModel viewModel, IElasticIpRepository elasticIp,
            AwsConnectionSettings awsConnectionSettings, ToolkitContext toolkitContext)
            : base(viewModel, elasticIp, awsConnectionSettings, toolkitContext)
        {
        }

        protected override bool CanExecuteCore(SelectedElasticIpCommandArgs args)
        {
            return !args.SelectedAddress.IsAddressInUse;
        }

        protected override async Task<bool> PromptAsync(SelectedElasticIpCommandArgs args)
        {
            var model = new AssociateAddressModel(args.SelectedAddress);
            model.AvailableInstances =
                (await _elasticIp.GetUnassociatedInstancesAsync(args.SelectedAddress.Domain)).ToList();

            if (model.AvailableInstances.Count == 0)
            {
                throw new Ec2Exception(
                    "There are either no running EC2 instances that can be associated with an Elastic IP, or all running instances are already associated with one.",
                    Ec2Exception.Ec2ErrorCode.NoInstances);
            }

            model.Instance = model.AvailableInstances.FirstOrDefault();

            var control = new AssociateAddressControl(model);
            var result = _toolkitContext.ToolkitHost.ShowModal(control);

            if (result)
            {
                _instanceId = model.Instance.InstanceId;
            }

            return result;
        }

        protected override async Task ExecuteAsync(SelectedElasticIpCommandArgs args)
        {
            await _elasticIp.AssociateWithInstance(args.SelectedAddress, _instanceId);
            await RefreshElasticIpsAsync();

            var addressToSelect = _viewModel.Addresses.FirstOrDefault(x => x.PublicIp == args.SelectedAddress.PublicIp);
            if (addressToSelect != null)
            {
                args.Grid.SelectAndScrollIntoView(addressToSelect);
            }
        }

        protected override void HandleExecuteException(Exception ex)
        {
            _logger.Error("Error associating Elastic IP", ex);
            _toolkitContext.ToolkitHost.ShowError("Associate Elastic IP Error",
                $"Error associating address: {ex.Message}");
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
