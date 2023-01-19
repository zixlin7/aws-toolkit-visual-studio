using System;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;

namespace Amazon.AWSToolkit.Commands
{
    /// <summary>
    /// Base class representing the inputs provided to a <see cref="PromptAndExecuteHandler{TArgs}"/>
    /// when it is invoked.
    /// </summary>
    public class PromptAndExecuteHandlerArgs
    {
    }

    /// <summary>
    /// Provides <see cref="PromptAndExecuteHandler{TArgs}"/> handlers with data they
    /// can execute throughout the lifecycle of the handler.
    /// 
    /// Handlers may derive a state class if they need access to more data, or can use this one.
    /// </summary>
    public class PromptAndExecuteHandlerState
    {
        public ToolkitContext ToolkitContext { get; }

        public PromptAndExecuteHandlerState(ToolkitContext toolkitContext)
        {
            Arg.NotNull(toolkitContext, nameof(toolkitContext));
            ToolkitContext = toolkitContext;
        }
    }

    /// <summary>
    /// An operation to be implemented, that can be orchestrated by a Command
    /// (eg: <seealso cref="PromptAndExecuteCommand{THandlerArgs}"/>).
    ///
    /// Handlers are not concerned with how they are invoked, but contain the logic required
    /// to perform an operation.
    ///
    /// This class provides conveniences:
    /// - handler defaults to always execute
    /// - handler defaults to not requiring a prompt
    /// </summary>
    /// <typeparam name="TArgs">Type of object provided to the handler when it is invoked.</typeparam>
    public abstract class PromptAndExecuteHandler<TArgs> where TArgs : PromptAndExecuteHandlerArgs
    {
        /// <summary>
        /// Converts a generic (data-bound) object into the expected argument type.
        /// </summary>
        public abstract TArgs AsHandlerArgs(object parameter);

        /// <summary>
        /// Returns whether the handler can be executed or not.
        /// </summary>
        /// <param name="args">Argument passed to <see cref="ExecuteAsync"/>.</param>
        /// <returns>true if this handler can be executed; otherwise, false.</returns>
        public virtual bool CanExecute(TArgs args)
        {
            return true;
        }

        /// <summary>
        /// Prompts the user about this operation, prior to performing it.
        /// </summary>
        /// <param name="args">
        /// Data to operate on or with.
        /// This is conventionally the input that has been data-bound to the orchestration command.
        /// </param>
        /// <returns>true: operation should proceed, false: operation should be cancelled</returns>
        public virtual Task<bool> PromptAsync(TArgs args)
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// The operation being implemented.
        /// </summary>
        /// <param name="args">
        /// Data to operate on or with.
        /// This is conventionally the input that has been data-bound to a command.
        /// </param>
        public abstract Task ExecuteAsync(TArgs args);

        /// <summary>
        /// A handler for processing when an error occurs during the handler's orchestration.
        /// </summary>
        public abstract void HandleExecuteException(Exception ex);

        /// <summary>
        /// A hook that allows implementing handlers to record metric(s) based on the operation that was executed.
        /// This is called on completion of the command (pass, fail, or cancel).
        /// </summary>
        public abstract void RecordMetric(ToolkitCommandResult result);
    }
}
