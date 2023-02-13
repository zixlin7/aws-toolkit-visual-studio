using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View.DataGrid;
using Amazon.AWSToolkit.EC2.ViewModels;

namespace Amazon.AWSToolkit.EC2.Commands
{
    public class InstanceCommandArgs
    {
        public ICustomizeColumnGrid Grid;
    }

    /// <summary>
    /// This is a base command that can be used with EC2 Instance operations coming from the ViewInstancesControl grid.
    /// The command is structured such that:
    /// 1 - users are prompted for some kind of input. This is optional, and can be bypassed if not applicable.
    /// 2 - the operation is performed
    /// 3 - telemetry is logged in relation to the operation
    /// </summary>
    public abstract class InstanceCommand : BaseEc2Command
    {
        protected ViewInstancesViewModel _viewModel;

        protected InstanceCommand(ViewInstancesViewModel viewModel,
            AwsConnectionSettings awsConnectionSettings, ToolkitContext toolkitContext)
            : base(awsConnectionSettings, toolkitContext)
        {
            _viewModel = viewModel;
        }

        protected override bool CanExecuteCore(object parameter)
        {
            return TryGetCommandArgs(parameter, out var args)
                   && args.Grid.SelectedItems.Count == 1
                   && CanExecuteCore(args);
        }

        protected virtual bool CanExecuteCore(InstanceCommandArgs args)
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

        protected virtual Task<bool> PromptAsync(InstanceCommandArgs args)
        {
            return Task.FromResult(Prompt(args));
        }

        protected virtual bool Prompt(InstanceCommandArgs args)
        {
            return true;
        }

        protected override Task ExecuteAsync(object parameter)
        {
            if (!TryGetCommandArgs(parameter, out var args))
            {
                // We screwed up the logic somewhere
                throw new Ec2Exception("Unable to find the EC2 Instance data", Ec2Exception.Ec2ErrorCode.InternalMissingEc2State);
            }

            return ExecuteAsync(args);
        }

        protected abstract Task ExecuteAsync(InstanceCommandArgs args);

        private bool TryGetCommandArgs(object parameter, out InstanceCommandArgs commandArgs)
        {
            commandArgs = null;

            if (parameter is ICustomizeColumnGrid grid)
            {
                commandArgs = new InstanceCommandArgs { Grid = grid };
                return true;
            }

            return false;
        }
    }
}
