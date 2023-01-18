using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Collections;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.Repositories;

namespace Amazon.AWSToolkit.EC2.Commands
{
    /// <summary>
    /// This is a base command that can be used with Elastic IP operations coming from the ViewElasticIPsControl grid.
    /// The command is structured such that:
    /// 1 - users are prompted for some kind of input. This is optional, and can be bypassed if not applicable.
    /// 2 - the operation is performed
    /// 3 - telemetry is logged in relation to the operation
    ///
    /// Commands that operate based on a selected Elastic IP item from the grid can derive from <see cref="SelectedElasticIpCommand"/>
    /// </summary>
    public abstract class ElasticIpCommand : BaseEc2Command
    {
        protected IElasticIpRepository _elasticIp;
        protected ViewElasticIPsModel _viewModel;

        protected ElasticIpCommand(ViewElasticIPsModel viewModel, IElasticIpRepository elasticIp,
            AwsConnectionSettings awsConnectionSettings, ToolkitContext toolkitContext)
            : base(awsConnectionSettings, toolkitContext)
        {
            _elasticIp = elasticIp;
            _viewModel = viewModel;
        }

        /// <summary>
        /// Utility method to reload the account's Elastic Ip objects into the viewmodel
        /// </summary>
        protected async Task RefreshElasticIpsAsync()
        {
            var addresses = (await _elasticIp.ListElasticIpsAsync()).ToList();

            _toolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
            {
                _viewModel.Addresses.Clear();
                _viewModel.Addresses.AddAll(addresses);
            });
        }
    }
}
