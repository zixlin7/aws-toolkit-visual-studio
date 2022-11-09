using System;

namespace Amazon.AWSToolkit.Commands
{
    /// <summary>
    /// Utility class allowing for easy WPF DataBinding to functionality.
    /// </summary>
    public class RelayCommand : Command
    {
        private readonly Func<object, bool> _canExecute;
        private readonly Action<object> _execute;

        public RelayCommand(Action<object> execute)
            : this(null, execute) { }

        public RelayCommand(Func<object, bool> canExecute, Action<object> execute)
        {
            _canExecute = canExecute;
            _execute = execute;
        }

        protected override bool CanExecuteCore(object parameter)
        {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        protected override void ExecuteCore(object parameter)
        {
            _execute?.Invoke(parameter);
        }
    }
}
