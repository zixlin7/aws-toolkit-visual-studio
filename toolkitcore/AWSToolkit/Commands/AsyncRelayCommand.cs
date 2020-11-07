using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Amazon.AWSToolkit.Tasks;

namespace Amazon.AWSToolkit.Commands
{
    public interface IAsyncCommand : ICommand
    {
        Task ExecuteAsync(object parameter);
    }

    /// <summary>
    /// Utility class allowing for easy WPF DataBinding to async-based functionality.
    /// </summary>
    public class AsyncRelayCommand : IAsyncCommand
    {
        public event EventHandler CanExecuteChanged;

        private readonly Func<object, bool> _canExecute;
        private readonly Func<object, Task> _execute;

        public AsyncRelayCommand(Func<object, Task> execute)
            : this(null, execute)
        {
        }

        public AsyncRelayCommand(Func<object, bool> canExecute, Func<object, Task> execute)
        {
            _canExecute = canExecute;
            _execute = execute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        public async Task ExecuteAsync(object parameter)
        {
            if (CanExecute(parameter))
            {
                await _execute(parameter);
            }

            RaiseCanExecuteChanged();
        }

        public void Execute(object parameter)
        {
            ExecuteAsync(parameter).LogExceptionAndForget();
        }

        private void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}