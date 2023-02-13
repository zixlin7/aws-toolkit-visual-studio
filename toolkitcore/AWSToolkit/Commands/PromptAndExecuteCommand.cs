using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;

namespace Amazon.AWSToolkit.Commands
{
    /// <summary>
    /// Async command implementation that orchestrates <see cref="PromptAndExecuteHandler{TArgs}"/> based operations.
    /// Handlers are invoked in the following pattern:
    /// 1 - users are prompted for some kind of input. This is optional, and can be bypassed if not applicable.
    /// 2 - the operation is performed
    /// 3 - telemetry is logged in relation to the operation
    ///
    /// Sealed - This class is not intended to be extended. If handlers need to be orchestrated in a different way,
    /// a different command implementation should be created.
    /// </summary>
    /// <typeparam name="THandlerArgs">Type of object provided to the handler when it is invoked.</typeparam>
    public sealed class PromptAndExecuteCommand<THandlerArgs> : AsyncCommand where THandlerArgs : PromptAndExecuteHandlerArgs
    {
        private readonly ToolkitContext _context;
        private readonly PromptAndExecuteHandler<THandlerArgs> _handler;

        public PromptAndExecuteCommand(PromptAndExecuteHandler<THandlerArgs> handler, ToolkitContext context)
        {
            Arg.NotNull(handler, nameof(handler));
            Arg.NotNull(context, nameof(context));

            _handler = handler;
            _context = context;
        }

        protected override bool CanExecuteCore(object parameter)
        {
            try
            {
                var args = _handler.AsHandlerArgs(parameter);
                return _handler.CanExecute(args);
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.Assert(!Debugger.IsAttached,
                    $"Handler {_handler.GetType().Name} failed to convert parameter {parameter} to {typeof(THandlerArgs).Name}. This is a bug.",
                    ex.Message);
#endif
                return false;
            }
        }

        /// <summary>
        /// The main AsyncCommand implementation. The operation takes place, and then metrics are recorded.
        /// </summary>
        protected override async Task ExecuteCoreAsync(object parameter)
        {
            var result = await PromptAndExecuteAsync(parameter);
            _handler.RecordMetric(result);
        }

        /// <summary>
        /// Orchestrates the command handler.
        /// Users are prompted for something, and if they accept, an operation takes place.
        /// </summary>
        /// <returns>A result indicating success, failure (with exception), or cancel</returns>
        private async Task<ToolkitCommandResult> PromptAndExecuteAsync(object parameter)
        {
            try
            {
                var args = _handler.AsHandlerArgs(parameter);

                if (!await _handler.PromptAsync(args))
                {
                    return ToolkitCommandResult.CreateCancelled();
                }

                await _handler.ExecuteAsync(args);

                return ToolkitCommandResult.CreateSucceeded();
            }
            catch (Exception ex)
            {
                _handler.HandleExecuteException(ex);
                return ToolkitCommandResult.CreateFailed(ex);
            }
        }
    }
}
