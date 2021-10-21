using System;
using System.Windows.Input;

namespace Amazon.AWSToolkit.Commands
{
    /// <summary>
    /// Exception when there is a problem executing a Command.
    /// <see cref="ICommand"/>
    /// </summary>
    public class CommandException : Exception
    {
        public CommandException(string message, Exception e) : base(message, e)
        {

        }
    }
}
