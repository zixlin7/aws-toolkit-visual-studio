using System;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.Repositories;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.EC2.View.DataGrid;
using Amazon.AWSToolkit.Navigator;

using log4net;

namespace Amazon.AWSToolkit.EC2.Commands
{
    public class AllocateElasticIpCommand : ElasticIpCommand
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(AllocateElasticIpCommand));

        private string _ipDomain;

        public AllocateElasticIpCommand(ViewElasticIPsModel viewModel, IElasticIpRepository elasticIp,
            AwsConnectionSettings awsConnectionSettings, ToolkitContext toolkitContext)
            : base(viewModel, elasticIp, awsConnectionSettings, toolkitContext)
        {
        }

        protected override bool CanExecuteCore(object parameter)
        {
            return parameter is ICustomizeColumnGrid;
        }

        protected override bool Prompt(object _)
        {
            var control = new AllocateAddressControl();
            if (!_toolkitContext.ToolkitHost.ShowModal(control))
            {
                return false;
            }

            _ipDomain = control.Domain;
            return true;
        }

        protected override async Task ExecuteAsync(object parameter)
        {
            var grid = parameter as ICustomizeColumnGrid;
            Arg.NotNull(grid, nameof(parameter));

            var publicId = await _elasticIp.AllocateElasticIpAsync(_ipDomain, _awsConnectionSettings);

            await RefreshElasticIpsAsync();

            var addressToSelect = _viewModel.Addresses.FirstOrDefault(x => x.PublicIp == publicId);
            if (addressToSelect != null)
            {
                grid.SelectAndScrollIntoView(addressToSelect);
            }
        }

        protected override void HandleExecuteException(Exception ex)
        {
            _logger.Error("Error allocating Elastic IP", ex);
            _toolkitContext.ToolkitHost.ShowError("Allocate Elastic IP Error", "Error allocating Elastic IP: " + ex.Message);
        }

        protected override void RecordMetric(ActionResults result)
        {
            var data = CreateMetricData<Ec2CreateElasticIp>(result);
            data.Result = result.AsTelemetryResult();

            _toolkitContext.TelemetryLogger.RecordEc2CreateElasticIp(data);
        }
    }
}
