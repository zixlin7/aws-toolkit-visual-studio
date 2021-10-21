using System;
using System.Windows.Input;

using Amazon.AWSToolkit.Shared;

using log4net;

namespace Amazon.AWSToolkit.Commands
{
    /// <summary>
    /// Error handling command decorator that shows error to user and logs the error.
    /// </summary>
    public class ShowExceptionAndForgetCommand : ICommand
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(ShowExceptionAndForgetCommand));

        public event EventHandler CanExecuteChanged;

        private readonly ICommand _command;
        private readonly IAWSToolkitShellProvider _toolkitHost;

        public ShowExceptionAndForgetCommand(ICommand command, IAWSToolkitShellProvider toolkitHost)
        {
            _command = command;
            _toolkitHost = toolkitHost;
        }

        public bool CanExecute(object parameter) => _command.CanExecute(parameter);

        public void Execute(object parameter)
        {
            try
            {
                _command.Execute(parameter);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                ShowErrorToUser(e);
            }
        }

        private void ShowErrorToUser(Exception e)
        {
            if (e.InnerException != null)
            {
                _toolkitHost.ShowError(e.Message, e.InnerException.Message);
            }
            else
            {
                _toolkitHost.ShowError(e.Message);
            }
        }
    }
}
