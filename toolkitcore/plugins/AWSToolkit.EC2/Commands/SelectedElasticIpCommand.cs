using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.Repositories;
using Amazon.AWSToolkit.EC2.View.DataGrid;

namespace Amazon.AWSToolkit.EC2.Commands
{
    /// <summary>
    /// Commands working with a an Elastic IP that is selected in the ViewElasticIPsControl grid can implement this class.
    /// It provides type-checked access to the selected address.
    /// </summary>
    public abstract class SelectedElasticIpCommand : ElasticIpCommand
    {
        protected SelectedElasticIpCommand(ViewElasticIPsModel viewModel, IElasticIpRepository elasticIp,
            AwsConnectionSettings awsConnectionSettings, ToolkitContext toolkitContext)
            : base(viewModel, elasticIp, awsConnectionSettings, toolkitContext)
        {
        }

        protected override bool CanExecuteCore(object parameter)
        {
            return parameter is ICustomizeColumnGrid grid
                   && grid.SelectedItems.Count == 1
                   && grid.SelectedItem is AddressWrapper selectedAddress
                   && !selectedAddress.IsAddressInUse;
        }

        protected override bool Prompt(object parameter)
        {
            return Prompt(GetAddress(parameter));
        }

        protected virtual bool Prompt(AddressWrapper address)
        {
            return true;
        }

        protected override Task ExecuteAsync(object parameter)
        {
            return ExecuteAsync(GetAddress(parameter));
        }

        protected abstract Task ExecuteAsync(AddressWrapper address);

        private AddressWrapper GetAddress(object parameter)
        {
            var grid = parameter as ICustomizeColumnGrid;
            return grid.SelectedItem as AddressWrapper;
        }
    }
}
