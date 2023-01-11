using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.Repositories;
using Amazon.AWSToolkit.EC2.View.DataGrid;

namespace Amazon.AWSToolkit.EC2.Commands
{
    public class SelectedElasticIpCommandArgs
    {
        public ICustomizeColumnGrid Grid;
        public AddressWrapper SelectedAddress;
    }

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
            return TryGetCommandArgs(parameter, out var args)
                   && args.Grid.SelectedItems.Count == 1
                   && CanExecuteCore(args);
        }

        protected virtual bool CanExecuteCore(SelectedElasticIpCommandArgs args)
        {
            return true;
        }

        protected override Task<bool> PromptAsync(object parameter)
        {
            if (!TryGetCommandArgs(parameter, out var args))
            {
                return Task.FromResult(false);
            }

            return PromptAsync(args);
        }

        protected override bool Prompt(object parameter)
        {
            if (!TryGetCommandArgs(parameter, out var args))
            {
                return false;
            }

            return Prompt(args);
        }

        protected virtual Task<bool> PromptAsync(SelectedElasticIpCommandArgs args)
        {
            return Task.FromResult(Prompt(args));
        }

        protected virtual bool Prompt(SelectedElasticIpCommandArgs args)
        {
            return true;
        }

        protected override Task ExecuteAsync(object parameter)
        {
            if (!TryGetCommandArgs(parameter, out var args))
            {
                // We screwed up the logic somewhere
                throw new Ec2Exception("Unable to find the selected Elastic IP", Ec2Exception.Ec2ErrorCode.InternalMissingEc2State);
            }

            return ExecuteAsync(args);
        }

        protected abstract Task ExecuteAsync(SelectedElasticIpCommandArgs args);

        private bool TryGetCommandArgs(object parameter, out SelectedElasticIpCommandArgs commandArgs)
        {
            commandArgs = null;

            if (parameter is ICustomizeColumnGrid grid
                && grid.SelectedItem is AddressWrapper selectedAddress)
            {
                commandArgs = new SelectedElasticIpCommandArgs { Grid = grid, SelectedAddress = selectedAddress, };
                return true;
            }

            return false;
        }
    }
}
