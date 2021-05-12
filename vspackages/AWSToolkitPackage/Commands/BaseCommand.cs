using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

using log4net;

using Microsoft;
using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;

namespace Amazon.AWSToolkit.VisualStudio.Commands
{
    /// <summary>
    /// Abstract class responsible for creating extension commands that
    /// are defined in the vsct file and hooked up to Menu Items.
    ///
    /// During extension initialization, the derived class InitializeAsync calls
    /// should be called to register commands.
    ///
    /// This class handles the menu item's state events, and supports
    /// invocation in sync and async modes.
    /// </summary>
    /// <typeparam name="T">Derived class where command is implemented</typeparam>
    public abstract class BaseCommand<T> where T : class
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(BaseCommand<T>));

        /// <summary>
        /// The VS Extension Package this command is registered to
        /// </summary>
        public AsyncPackage Package { get; private set; }

        /// <summary>
        /// The Command invoked by VS Menus
        /// </summary>
        protected OleMenuCommand Command { get; private set; }

        /// <summary>
        /// Implementing classes are responsible for instantiating themselves
        /// </summary>
        public delegate BaseCommand<T> CreateCommand();

        /// <summary>
        /// Instantiates a command and associates it with a VS Menu.
        /// </summary>
        /// <param name="createCommand">Delegate that instantiates the derived command object</param>
        /// <param name="menuGroup">The menu group this command belongs to (VSCT)</param>
        /// <param name="commandId">The command identifier (VSCT)</param>
        /// <param name="package">Visual Studio Extension that owns this command</param>
        /// <returns>The created command, null if there was an error</returns>
        public static async Task<T> InitializeAsync(
            CreateCommand createCommand,
            Guid menuGroup, int commandId,
            AsyncPackage package)
        {
            try
            {
                var commandIdentifier = new CommandID(menuGroup, commandId);

                var command = createCommand();
                Assumes.Present(command);

                command.Package = package;
                command.Command = new OleMenuCommand(command.Execute, commandIdentifier);
                command.Command.BeforeQueryStatus += (sender, args) => { command.BeforeQueryStatus(sender, args); };

                // IMenuCommandService requires UI Thread
                await package.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

                var commandService = (IMenuCommandService) await package.GetServiceAsync(typeof(IMenuCommandService));
                Assumes.Present(commandService);

                commandService.AddCommand(command.Command);

                return command as T;
            }
            catch (Exception e)
            {
                Logger.Error("Unable to set up Extension Command", e);
                return null;
            }
        }

        /// <summary>
        /// Async implementation of the Command.
        /// Implementing classes override this function if possible,
        /// otherwise they must override <see cref="Execute"/>.
        /// </summary>
        protected virtual Task ExecuteAsync(object sender, OleMenuCmdEventArgs args)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Non-async implementation of the Command.
        /// If derived classes have an async implementation (<see cref="ExecuteAsync"/>),
        /// they should not override this method.
        /// </summary>
        protected virtual void Execute(object sender, EventArgs args)
        {
            Package?.JoinableTaskFactory.RunAsync(async delegate
            {
                try
                {
                    await ExecuteAsync(sender, (OleMenuCmdEventArgs) args);
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to run command", e);
                }
            });
        }

        /// <summary>
        /// Queried by Visual Studio prior to potentially showing this command in a menu.
        /// </summary>
        private void BeforeQueryStatus(object sender, EventArgs args)
        {
            if (!(sender is OleMenuCommand menuCommand))
            {
                return;
            }

            BeforeQueryStatus(menuCommand, args);
        }

        /// <summary>
        /// Callback prior to menu rendering controlling visual aspects of the command.
        /// </summary>
        protected virtual void BeforeQueryStatus(OleMenuCommand menuCommand, EventArgs args)
        {
            // Derived classes implement this method if necessary
        }
    }
}
