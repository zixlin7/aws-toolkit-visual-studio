using System.Threading.Tasks;

using Amazon.AWSToolkit.Tasks;

namespace Amazon.AWSToolkit.Commands
{
    /// <summary>
    /// Base class for async commands that provides CanExecuteChanged handling.
    /// </summary>
    /// <remarks>
    /// When implementing a subclass, implementors need only override the ExecuteCoreAsync method.  CanExecuteCore may be
    /// overriden as well in cases where the default implementation that always returns true is not desireable.
    /// </remarks>
    public abstract class AsyncCommand : Command
    {
        /// <summary>
        /// Executes the async commmand synchronously.
        /// </summary>
        /// <param name="parameter">Argument to pass to command execution.</param>
        /// <remarks>
        /// This method returns immediately.  Any exceptions thrown are logged and forgotten.
        /// </remarks>
        protected override void ExecuteCore(object parameter)
        {
            ExecuteCoreAsync(parameter).LogExceptionAndForget();
        }

        /// <summary>
        /// Executes the async command.
        /// </summary>
        /// <param name="parameter">Argument to pass to command execution.</param>
        public async Task ExecuteAsync(object parameter = null)
        {
            if (CanExecute(parameter))
            {
                await ExecuteCoreAsync(parameter);
                CanExecute(parameter);
            }
        }

        /// <summary>
        /// Derived classes should override this method to implement async logic of command.
        /// </summary>
        /// <param name="parameter">Argument passed to ExecuteAsync.</param>
        /// <remarks>
        /// When overriding, only provide the logic necessary to perform the command.  CanExecute will implicitly
        /// be called after this method is called, so there is no need to call CanExecute or raise CanExecuteChanged
        /// from within this method.
        /// </remarks>
        protected abstract Task ExecuteCoreAsync(object parameter);
    }
}
