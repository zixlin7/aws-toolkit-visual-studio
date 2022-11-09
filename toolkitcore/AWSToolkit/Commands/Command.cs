using System;
using System.Windows.Input;

namespace Amazon.AWSToolkit.Commands
{
    /// <summary>
    /// Base class for commands that provides CanExecuteChanged handling.
    /// </summary>
    /// <remarks>
    /// When implementing a subclass, implementors need only override the ExecuteCore method.  CanExecuteCore may be
    /// overriden as well in cases where the default implementation that always returns true is not desireable.
    /// </remarks>
    public abstract class Command : ICommand
    {
        private bool _previousCanExecute;

        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute.
        /// </summary>
        /// <remarks>
        /// TL;DR - When using command binding from a WPF control, nothing to worry about.  When explicitly adding a handler
        /// to this event, it is your responsibility to later remove that handler instance to prevent a memory leak.
        /// 
        /// When using commands from WPF controls, typically a weak event manager is used for when adding handlers
        /// to events.  When explicitly adding handlers to this event from code that doesn't use a weak event manager,
        /// it is necessary for the calling code to subsequently remove the handlers when no longer needed to prevent
        /// a memory leak leading to long-lived objects.  This is the standard behavior for events in .NET.
        ///
        /// https://learn.microsoft.com/en-us/dotnet/api/system.windows.input.canexecutechangedeventmanager?view=netframework-4.7.2
        /// https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/weak-event-patterns?view=netframeworkdesktop-4.8
        /// </remarks>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Base class constructor.
        /// </summary>
        /// <remarks>
        /// This class also listens for the WPF CommandManager.RequerySuggested event and raises its own
        /// CanExecuteChanged event when this happens.
        /// 
        /// CommandManager.RequertySuggested uses a weak event manager to prevent the handlers from being
        /// long-lived objects (i.e. there isn't a need to explicitly remove the handlers once added.)
        /// 
        /// https://learn.microsoft.com/en-us/dotnet/api/system.windows.input.commandmanager.requerysuggested?view=netframework-4.7.2
        /// https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/weak-event-patterns?view=netframeworkdesktop-4.8
        /// </remarks>
        protected Command()
        {
            CommandManager.RequerySuggested += (sender, e) => OnCanExecuteChanged(e);
        }

        /// <summary>
        /// Determines if command can be executed or not.  Raises OnChangedEvent if return value has changed since previous call to this method.
        /// </summary>
        /// <param name="parameter">Data used by the command. If the command does not require data to be passed, this object can be set to null.</param>
        /// <returns>true if this command can be executed; otherwise, false.</returns>
        public bool CanExecute(object parameter = null)
        {
            var canExecute = CanExecuteCore(parameter);

            if (canExecute != _previousCanExecute)
            {
                _previousCanExecute = canExecute;
                OnCanExecuteChanged();
            }

            return canExecute;
        }

        /// <summary>
        /// Returns whether the command can be executed or not.
        /// </summary>
        /// <param name="parameter">Argument passed to CanExecute.</param>
        /// <returns>true if this command can be executed; otherwise, false.</returns>
        /// <remarks>
        /// Override this method in derived classes when the base implementation of always returning true is
        /// not desireable.
        /// </remarks>
        protected virtual bool CanExecuteCore(object parameter)
        {
            return true;
        }

        /// <summary>
        /// Executes the command if CanExecute returns true.
        /// </summary>
        /// <param name="parameter">Data used by the command. If the command does not require data to be passed, this object can be set to null.</param>
        public void Execute(object parameter = null)
        {
            if (CanExecute(parameter))
            {
                ExecuteCore(parameter);
                CanExecute(parameter);
            }
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="parameter">Argument passed to Execute</param>
        /// <remarks>
        /// When overriding, only provide the logic necessary to perform the command.  CanExecute will implicitly
        /// be called after this method is called, so there is no need to call CanExecute or raise CanExecuteChanged
        /// from within this method.
        /// </remarks>
        protected abstract void ExecuteCore(object parameter);

        /// <summary>
        /// Raises the CanExecuteChanged event.
        /// </summary>
        /// <param name="e">The EventArgs to pass when raised.</param>
        /// <remarks>
        /// When overriding this method in a derived class, call this base implementation to raise the CanExecuteChanged
        /// event.
        ///
        /// https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/event
        /// </remarks>
        protected virtual void OnCanExecuteChanged(EventArgs e = null)
        {
            CanExecuteChanged?.Invoke(this, e ?? EventArgs.Empty);
        }
    }
}
