using System;
using System.Windows.Input;

namespace Amazon.AWSToolkit.Commands
{
    /// <summary>
    /// Utility class allowing for easy WPF DataBinding to functionality.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private event EventHandler CanExecuteChangedInternal;
        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
                CanExecuteChangedInternal += value;
            }
            remove
            {
                CommandManager.RequerySuggested -= value;
                CanExecuteChangedInternal -= value;
            }
        }

        private readonly Func<object, bool> _canExecute;
        private readonly Action<object> _execute;

        public RelayCommand(Action<object> execute)
            : this(null, execute)
        {
        }

        public RelayCommand(Func<object, bool> canExecute, Action<object> execute)
        {
            _canExecute = canExecute;
            _execute = execute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                _execute?.Invoke(parameter);
            }

            RaiseCanExecuteChanged();
        }

        private void RaiseCanExecuteChanged()
        {
            CanExecuteChangedInternal?.Invoke(this, EventArgs.Empty);
        }
    }
}
