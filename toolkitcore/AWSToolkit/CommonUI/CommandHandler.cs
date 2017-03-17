using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Amazon.AWSToolkit.CommonUI
{
    /// <summary>
    /// Basic ICommand wrapper enabling WPF command binding.
    /// </summary>
    public class CommandHandler : ICommand
    {
        private readonly Action _action;
        private readonly bool _canExecute;

        public CommandHandler(Action action, bool canExecute)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            _action();
        }
    }
}
