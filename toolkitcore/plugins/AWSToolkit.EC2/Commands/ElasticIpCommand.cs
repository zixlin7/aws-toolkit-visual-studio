using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Collections;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.Repositories;
using Amazon.AWSToolkit.EC2.View.DataGrid;

namespace Amazon.AWSToolkit.EC2.Commands
{
    internal class ElasticIpCommandArgs : PromptAndExecuteHandlerArgs
    {
        public ICustomizeColumnGrid Grid;

        public AddressWrapper GetSelectedAddress()
        {
            return Grid.SelectedItem as AddressWrapper;
        }

        internal static ElasticIpCommandArgs FromParameter(object parameter)
        {
            if (parameter is ICustomizeColumnGrid grid)
            {
                return new ElasticIpCommandArgs { Grid = grid, };
            }

            // We screwed up the logic somewhere
            throw new Ec2Exception("Unable to find Elastic IP details",
                Ec2Exception.Ec2ErrorCode.InternalMissingEc2State);
        }
    }

    internal class ElasticIpCommandState : PromptAndExecuteHandlerState
    {
        public AwsConnectionSettings AwsConnectionSettings { get; }
        public IElasticIpRepository ElasticIpRepository { get; }
        public ViewElasticIPsModel ViewElasticIPsModel { get; }

        public ElasticIpCommandState(ViewElasticIPsModel viewElasticIPsModel, IElasticIpRepository elasticIpRepository,
            AwsConnectionSettings awsConnectionSettings, ToolkitContext toolkitContext) : base(toolkitContext)
        {
            AwsConnectionSettings = awsConnectionSettings;
            ElasticIpRepository = elasticIpRepository;
            ViewElasticIPsModel = viewElasticIPsModel;
        }

        /// <summary>
        /// Utility method to reload the account's Elastic Ip objects into the viewmodel
        /// </summary>
        public async Task RefreshElasticIpsAsync()
        {
            var addresses = (await ElasticIpRepository.ListElasticIpsAsync()).ToList();

            ToolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
            {
                ViewElasticIPsModel.Addresses.Clear();
                ViewElasticIPsModel.Addresses.AddAll(addresses);
            });
        }
    }
}
