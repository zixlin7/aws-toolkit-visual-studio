using System;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.Navigator
{
    /// <summary>
    /// ICommand wrapper enabling WPF command binding for Navigator connection messaging panel
    /// </summary>
    public class NavigatorCommand : CommandHandler
    {
        public string DisplayName { get; set; }

        public NavigatorCommand(string displayName, Action action, bool canExecute) : this(action, canExecute)
        {
            DisplayName = displayName;
        }

        public NavigatorCommand(Action action, bool canExecute) : base(action, canExecute)
        {
        }
    }
}
